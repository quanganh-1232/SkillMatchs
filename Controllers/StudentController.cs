using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkillMatch.Controllers
{
    // BỎ [Authorize] ở đây để Client có thể vào xem Action Profile công khai
    public class StudentController : Controller
    {
        private readonly SkillMatchDbContext _context;

        public StudentController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // ============================================================================
        // 1. ACTION: XEM PROFILE CỦA CHÍNH MÌNH (CHỈ CHO SINH VIÊN ĐANG ĐĂNG NHẬP)
        // ============================================================================
        [Authorize(Roles = "Student")] // Chỉ cho phép sinh viên vào cấu hình/xem trang cá nhân của mình
        public async Task<IActionResult> Index()
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            return await GenerateProfileView(studentId, isOwnProfile: true);
        }

        // ============================================================================
        // 2. ACTION MỚI: XEM PROFILE CỦA SINH VIÊN BẤT KỲ (CHO CLIENT/GUEST ĐIỀU HƯỚNG SANG)
        // ============================================================================
        public async Task<IActionResult> Profile(int id)
        {
            // Tận dụng hàm chung để render dữ liệu profile dựa theo ID truyền từ trang chủ lên
            return await GenerateProfileView(id, isOwnProfile: false);
        }

        /// <summary>
        /// Hàm bổ trợ dùng chung để nạp và tính toán dữ liệu Profile nhằm tránh lặp code
        /// </summary>
        private async Task<IActionResult> GenerateProfileView(int studentId, bool isOwnProfile)
        {
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null || student.Role != "Student") return NotFound();

            // Giải mã chuỗi JSON từ trường Skills
            StudentProfileData profileData;
            try
            {
                profileData = JsonSerializer.Deserialize<StudentProfileData>(student.Skills ?? "{}") ?? new StudentProfileData();
            }
            catch
            {
                profileData = new StudentProfileData();
            }

            profileData.SkillList ??= new List<SkillProgress>();
            profileData.Services ??= new List<StudentService>();
            profileData.Portfolio ??= new List<PortfolioProject>();
            if (string.IsNullOrEmpty(profileData.School))
            {
                profileData.School = "Chưa cập nhật thông tin trường học";
            }

            // Truy vấn đánh giá thực tế từ nhà tuyển dụng
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Job)
                .Where(f => _context.Applications.Any(a => a.StudentId == studentId && a.JobId == f.JobId && a.Status == "Accepted"))
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            ViewBag.ProfileData = profileData;
            ViewBag.Feedbacks = feedbacks;
            ViewBag.AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0;
            ViewBag.IsOwnProfile = isOwnProfile; // Đánh dấu xem đây có phải chính chủ đang xem hay không

            return View("Index", student); // Cả 2 action dùng chung giao diện Index.cshtml
        }

        // ==========================================
        // 3. ACTION: XỬ LÝ LƯU CẬP NHẬT TẤT CẢ CÁC TAB
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateAdvancedProfile(
            string fullName, string bio, string school, string avatarUrl,
            string skillNames, string skillValues,
            string svcTitles, string svcPrices,
            string portTitles, string portImages)
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null) return NotFound();

            student.FullName = fullName;
            student.Avatar = avatarUrl;

            StudentProfileData oldProfile;
            try
            {
                oldProfile = JsonSerializer.Deserialize<StudentProfileData>(student.Skills ?? "{}") ?? new StudentProfileData();
            }
            catch
            {
                oldProfile = new StudentProfileData();
            }
            oldProfile.Services ??= new List<StudentService>();
            oldProfile.Portfolio ??= new List<PortfolioProject>();

            var oldServicesDict = oldProfile.Services
                .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .ToLookup(s => s.Title.Trim(), s => s)
                .ToDictionary(g => g.Key, g => g.First());

            var oldPortfolioDict = oldProfile.Portfolio
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .ToLookup(p => p.Title.Trim(), p => p)
                .ToDictionary(g => g.Key, g => g.First());

            var newProfileData = new StudentProfileData
            {
                Bio = bio ?? "",
                School = school ?? "",
                SkillList = new List<SkillProgress>(),
                Services = new List<StudentService>(),
                Portfolio = new List<PortfolioProject>()
            };

            // --- 1. XỬ LÝ GOM KỸ NĂNG ---
            var skNames = skillNames?.Split(',') ?? Array.Empty<string>();
            var skValues = skillValues?.Split(',') ?? Array.Empty<string>();
            for (int i = 0; i < skNames.Length; i++)
            {
                string name = skNames[i].Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    int.TryParse(skValues.ElementAtOrDefault(i), out int percent);
                    newProfileData.SkillList.Add(new SkillProgress
                    {
                        Name = name,
                        Percentage = percent <= 0 ? 100 : (percent > 100 ? 100 : percent)
                    });
                }
            }

            // --- 2. XỬ LÝ GOM DỊCH VỤ ---
            var sTitles = svcTitles?.Split('|') ?? Array.Empty<string>();
            var sPrices = svcPrices?.Split('|') ?? Array.Empty<string>();
            for (int i = 0; i < sTitles.Length; i++)
            {
                string tName = sTitles[i].Trim();
                if (!string.IsNullOrWhiteSpace(tName))
                {
                    string currentOrders = oldServicesDict.ContainsKey(tName) ? oldServicesDict[tName].OrdersCount : "0 đơn hàng";
                    double currentRating = oldServicesDict.ContainsKey(tName) ? oldServicesDict[tName].Rating : 5.0;

                    newProfileData.Services.Add(new StudentService
                    {
                        Title = tName,
                        PriceRange = sPrices.ElementAtOrDefault(i)?.Trim() ?? "Thỏa thuận",
                        OrdersCount = currentOrders,
                        Rating = currentRating
                    });
                }
            }

            // --- 3. XỬ LÝ GOM PORTFOLIO ---
            var pTitles = portTitles?.Split('|') ?? Array.Empty<string>();
            var pImages = portImages?.Split('|') ?? Array.Empty<string>();
            for (int i = 0; i < pTitles.Length; i++)
            {
                string pName = pTitles[i].Trim();
                if (!string.IsNullOrWhiteSpace(pName))
                {
                    string currentViews = oldPortfolioDict.ContainsKey(pName) ? oldPortfolioDict[pName].Views : "0";
                    string currentLikes = oldPortfolioDict.ContainsKey(pName) ? oldPortfolioDict[pName].Likes : "0";

                    newProfileData.Portfolio.Add(new PortfolioProject
                    {
                        Title = pName,
                        Views = currentViews,
                        Likes = currentLikes,
                        ImageUrl = pImages.ElementAtOrDefault(i)?.Trim() ?? "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe"
                    });
                }
            }

            student.Skills = JsonSerializer.Serialize(newProfileData);

            _context.Update(student);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hồ sơ năng lực đã được cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================================
        // 4. ACTION: QUẢN LÝ CÁC DỰ ÁN ĐÃ NHẬN / ĐÃ ỨNG TUYỂN
        // ============================================================================
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyApplications()
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            var applications = await _context.Applications
                .Include(a => a.Job)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CompleteJob(int applicationId, string productUrl)
        {
            if (string.IsNullOrWhiteSpace(productUrl))
            {
                TempData["ErrorMessage"] = "Vui lòng cung cấp đường dẫn (link) sản phẩm hợp lệ để nộp báo cáo!";
                return RedirectToAction(nameof(MyApplications));
            }

            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin dự án trên hệ thống.";
                return RedirectToAction(nameof(MyApplications));
            }

            bool isReSubmit = !string.IsNullOrEmpty(application.ProductUrl);
            application.ProductUrl = productUrl;
            application.Job.Status = "PendingApproval";

            _context.Update(application);
            _context.Update(application.Job);
            await _context.SaveChangesAsync();

            if (isReSubmit)
            {
                TempData["SuccessMessage"] = $"Đã cập nhật lại sản phẩm cho dự án '{application.Job.Title}' thành công!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã nộp sản phẩm dự án '{application.Job.Title}' thành công! Vui lòng đợi khách hàng kiểm tra và nghiệm thu.";
            }

            return RedirectToAction(nameof(MyApplications));
        }
    }
}