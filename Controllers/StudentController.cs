using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System.Text.Json;

namespace SkillMatch.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly SkillMatchDbContext _context;

        public StudentController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ACTION: HIỂN THỊ HỒ SƠ NĂNG LỰC ĐA TAB
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null) return NotFound();

            // Giải mã chuỗi JSON từ trường Skills có sẵn trong DB
            StudentProfileData profileData;
            try
            {
                profileData = JsonSerializer.Deserialize<StudentProfileData>(student.Skills ?? "{}") ?? new StudentProfileData();
            }
            catch
            {
                profileData = new StudentProfileData();
            }

            // Đảm bảo các thuộc tính không bị null để tránh lỗi ngoài View
            profileData.SkillList ??= new List<SkillProgress>();
            profileData.Services ??= new List<StudentService>();
            profileData.Portfolio ??= new List<PortfolioProject>();
            if (string.IsNullOrEmpty(profileData.School))
            {
                profileData.School = "Chưa cập nhật thông tin trường học";
            }

            // Truy vấn danh sách đánh giá thực tế từ nhà tuyển dụng dựa trên các Job mà sinh viên đã ứng tuyển thành công
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Job)
                .Where(f => _context.Applications.Any(a => a.StudentId == studentId && a.JobId == f.JobId && a.Status == "Accepted"))
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            ViewBag.ProfileData = profileData;
            ViewBag.Feedbacks = feedbacks;

            // Tính toán điểm đánh giá trung bình thực tế, nếu chưa có thì mặc định là 5.0
            ViewBag.AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0;

            return View(student);
        }

        // ==========================================
        // 2. ACTION: XỬ LÝ LƯU CẬP NHẬT TẤT CẢ CÁC TAB
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdvancedProfile(
            string fullName, string bio, string school, string avatarUrl, // ĐÃ BỔ SUNG: avatarUrl ở đây
            string skillNames, string skillValues,
            string svcTitles, string svcPrices,
            string portTitles, string portImages)
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null) return NotFound();

            // Thực hiện cập nhật dữ liệu cốt lõi của User
            student.FullName = fullName;
            student.Avatar = avatarUrl; // ĐÃ BỔ SUNG: Cập nhật đường dẫn ảnh trực tiếp vào thuộc tính của thực thể User

            // Lấy lại dữ liệu JSON cũ đang có trong DB để so sánh, giữ lại tương tác
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

            // Chuyển danh sách cũ sang Dictionary để tìm kiếm nhanh theo Tên (Title)
            var oldServicesDict = oldProfile.Services
                .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .ToLookup(s => s.Title.Trim(), s => s)
                .ToDictionary(g => g.Key, g => g.First());

            var oldPortfolioDict = oldProfile.Portfolio
                .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                .ToLookup(p => p.Title.Trim(), p => p)
                .ToDictionary(g => g.Key, g => g.First());

            // Khởi tạo đối tượng lưu trữ mới
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

            // Mã hóa đối tượng mới lại thành chuỗi JSON và cập nhật Database
            student.Skills = JsonSerializer.Serialize(newProfileData);

            _context.Update(student);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hồ sơ năng lực đã được cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================================
        // TÍNH NĂNG: QUẢN LÝ CÁC DỰ ÁN ĐÃ NHẬN / ĐÃ ỨNG TUYỂN
        // ============================================================================
        public async Task<IActionResult> MyApplications()
        {
            // Lấy ID sinh viên đang đăng nhập từ hệ thống định danh Claims
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            // Nạp danh sách Đơn ứng tuyển và nạp kèm (Include) thông tin Công việc liên quan
            var applications = await _context.Applications
                .Include(a => a.Job)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            // Kiểm tra xem đây là hành động nộp lần đầu hay nộp lại
            bool isReSubmit = !string.IsNullOrEmpty(application.ProductUrl);

            // LƯU / CẬP NHẬT LINK BÀI LÀM CỦA SINH VIÊN
            application.ProductUrl = productUrl;

            // CHUYỂN TRẠNG THÁI SANG: CHỜ KHÁCH HÀNG DUYỆT / NGHIỆM THU
            application.Job.Status = "PendingApproval";

            _context.Update(application);
            _context.Update(application.Job);
            await _context.SaveChangesAsync();

            // Thay đổi thông báo dựa vào việc nộp mới hay nộp lại
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