using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkillMatch.Controllers
{
    public class JobsController : Controller
    {
        private readonly SkillMatchDbContext _context;

        // Hàm khởi tạo tiêm trực tiếp Database Context vào controller
        public JobsController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // ============================================================================
        // VAI TRÒ: CHUNG (TẤT CẢ THÀNH VIÊN ĐỀU XEM ĐƯỢC)
        // ============================================================================

        // 1. Xem danh sách toàn bộ công việc (Hỗ trợ lọc theo Danh mục - CategoryId)
        public async Task<IActionResult> Index(int? categoryId)
        {
            // Nạp danh sách categories đưa ra giao diện làm bộ lọc
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategory = categoryId;

            // Truy vấn lấy danh sách Jobs
            var jobsQuery = _context.Jobs.AsQueryable();

            // Nếu người dùng chọn một danh mục cụ thể thì lọc theo danh mục đó
            if (categoryId.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.CategoryId == categoryId.Value);
            }

            var jobs = await jobsQuery.OrderByDescending(j => j.CreatedAt).ToListAsync();
            return View(jobs);
        }

        // 2. Xem chi tiết 1 công việc cụ thể (Kèm theo thông tin Tên Công ty & Tên Danh mục)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FirstOrDefaultAsync(m => m.Id == id);
            if (job == null) return NotFound();

            // Lấy thêm thông tin tên Danh mục đưa vào ViewBag
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == job.CategoryId);
            ViewBag.CategoryName = category?.Name ?? "Chưa phân loại";

            // Lấy thêm thông tin Khách hàng/Doanh nghiệp (ClientId) đăng bài này
            var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == job.ClientId);
            ViewBag.ClientName = client?.FullName ?? "Nhà tuyển dụng ẩn danh";

            return View(job);
        }


        // ============================================================================
        // VAI TRÒ: KHÁCH HÀNG (CLIENT)
        // ============================================================================
        // ============================================================================
        // TÍNH NĂNG MỚI: TRANG QUẢN LÝ DỰ ÁN DÀNH RIÊNG CHO CLIENT
        // ============================================================================
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Manage()
        {
            // 1. Lấy ID Nhà tuyển dụng đang đăng nhập từ Claims
            var clientIdClaim = User.FindFirst("UserId")?.Value;
            if (clientIdClaim == null) return Challenge();
            int clientId = int.Parse(clientIdClaim);

            // 2. Nạp toàn bộ danh sách Công việc của Client này kèm theo danh sách Đơn ứng tuyển và thông tin Sinh viên
            var myJobs = await _context.Jobs
                .Include(j => j.Applications)
                    .ThenInclude(a => a.Student) // Nạp thông tin tài khoản Sinh viên từ bảng Users
                .Where(j => j.ClientId == clientId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(myJobs);
        }

        // ============================================================================
        // TÍNH NĂNG MỚI: NGHIỆM THU DỰ ÁN VÀ LƯU ĐÁNH GIÁ (FEEDBACK)
        // ============================================================================
        // ============================================================================
        // NGHIỆM THU DỰ ÁN VÀ LƯU ĐÁNH GIÁ (FEEDBACK CHÍNH XÁC TỪ CLIENT)
        // ============================================================================
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAndFeedback(int jobId, int rating, string comment)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null) return NotFound();

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số sao đánh giá hợp lệ từ 1 đến 5.";
                return RedirectToAction(nameof(Manage));
            }

            // 1. Chuyển trạng thái công việc của dự án sang Completed (Hoàn thành)
            job.Status = "Completed";
            _context.Update(job);

            // 2. Lấy dữ liệu người dùng nhập từ giao diện để lưu vào bảng Feedbacks
            var feedback = new Feedback
            {
                JobId = jobId,
                Rating = rating,
                // Nếu người dùng xóa hết chữ thì mới dùng câu mặc định, còn không sẽ lưu đúng ý họ
                Comment = !string.IsNullOrWhiteSpace(comment) ? comment.Trim() : "Sản phẩm bàn giao đạt yêu cầu, hoàn thành đúng tiến độ!",
                CreatedAt = DateTime.Now
            };
            _context.Feedbacks.Add(feedback);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã nghiệm thu hoàn thành dự án '{job.Title}' và lưu đánh giá thành công!";
            return RedirectToAction(nameof(Manage));
        }
        // 3. Giao diện Đăng tin tuyển dụng (Chỉ Khách hàng được quyền truy cập)
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create()
        {
            // Truyền danh sách danh mục để đổ vào thẻ <select> lựa chọn trên giao diện
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // 4. Xử lý lưu tin tuyển dụng mới vào cơ sở dữ liệu
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Budget,Deadline,CategoryId")] Job job)
        {
            if (ModelState.IsValid)
            {
                // Lấy ID của Khách hàng đang đăng nhập từ hệ thống định danh cá nhân (Claims)
                var clientIdClaim = User.FindFirst("UserId")?.Value;
                if (clientIdClaim != null)
                {
                    job.ClientId = int.Parse(clientIdClaim);
                }

                job.Status = "Active"; // Mặc định trạng thái ban đầu là Active để sinh viên tìm thấy
                job.CreatedAt = DateTime.Now;

                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(job);
        }

        // 5. Khách hàng bấm nút "Duyệt nhận" (Hire) một sinh viên làm việc
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptApplicant(int applicationId)
        {
            // Khôi phục đơn ứng tuyển kèm theo dữ liệu liên kết bảng Jobs
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job == null) return NotFound();

            // Cập nhật trạng thái đơn ứng tuyển của sinh viên này thành Được nhận (Accepted)
            application.Status = "Accepted";

            // Chuyển đổi trạng thái công việc từ Active thành Processing (Đang thực hiện)
            application.Job.Status = "Processing";

            // Tự động chuyển tất cả các đơn ứng tuyển của các sinh viên khác trong dự án này thành Rejected (Từ chối)
            var otherApplications = await _context.Applications
                .Where(a => a.JobId == application.JobId && a.Id != applicationId)
                .ToListAsync();

            foreach (var other in otherApplications)
            {
                other.Status = "Rejected";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = application.JobId });
        }


        // ============================================================================
        // VAI TRÒ: NGƯỜI NHẬN VIỆC (STUDENT)
        // ============================================================================

        // 6. Xử lý Sinh viên nộp đơn ứng tuyển gửi kèm Thư giới thiệu (Cover Letter)
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId, string coverLetter)
        {
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            if (string.IsNullOrEmpty(coverLetter))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập thư giới thiệu bản thân trước khi nộp đơn!";
                return RedirectToAction("Details", new { id = jobId });
            }

            // Kiểm tra xem sinh viên này đã từng nộp đơn ứng tuyển dự án này chưa để tránh gửi trùng lặp dữ liệu
            var alreadyApplied = await _context.Applications
                .AnyAsync(a => a.JobId == jobId && a.StudentId == studentId);

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] = "Bạn đã gửi đơn ứng tuyển công việc này rồi!";
                return RedirectToAction("Details", new { id = jobId });
            }

            // Tạo thực thể Đơn ứng tuyển mới khớp hoàn hảo với cấu trúc bảng
            var app = new Application
            {
                JobId = jobId,
                StudentId = studentId,
                CoverLetter = coverLetter,
                Status = "Pending", // Trạng thái mặc định: Chờ duyệt tuyển
                AppliedAt = DateTime.Now
            };

            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ứng tuyển thành công! Vui lòng chờ phản hồi từ nhà tuyển dụng.";
            return RedirectToAction("Details", new { id = jobId });
        }
    }
}