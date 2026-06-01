using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System.Security.Claims;

namespace SkillMatch.Controllers
{
    public class JobsController : Controller
    {
        // Khai báo kết nối cơ sở dữ liệu
        private readonly SkillMatchDbContext _context;

        // Hàm khởi tạo để ép Database Context vào controller
        public JobsController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách toàn bộ công việc (Ai cũng xem được)
        public async Task<IActionResult> Index()
        {
            var jobs = await _context.Jobs.ToListAsync();
            return View(jobs);
        }

        // 2. Xem chi tiết 1 công việc cụ thể
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FirstOrDefaultAsync(m => m.Id == id);
            if (job == null) return NotFound();

            return View(job);
        }

        // 3. Giao diện Đăng tin tuyển dụng (Chỉ Khách hàng được vào)
        [Authorize(Roles = "Client")]
        public IActionResult Create()
        {
            return View();
        }

        // 4. Xử lý lưu tin tuyển dụng mới vào Database
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Budget,Deadline")] Job job)
        {
            if (ModelState.IsValid)
            {
                // Lấy ID của Khách hàng đang đăng nhập đưa vào Job
                var clientIdClaim = User.FindFirst("UserId")?.Value;
                if (clientIdClaim != null)
                {
                    job.ClientId = int.Parse(clientIdClaim);
                }

                job.Status = "Open";
                job.CreatedAt = DateTime.Now;

                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        // 5. ACTION: Xử lý Sinh viên gửi đơn ứng tuyển (Bị lỗi dấu ngoặc cũ của bạn đã sửa tại đây)
        [HttpPost]
        [Authorize(Roles = "Student")] // Chỉ sinh viên mới được ứng tuyển
        public async Task<IActionResult> Apply(int jobId, string coverLetter)
        {
            // Lấy ID của sinh viên đang đăng nhập từ Cookie
            var studentIdClaim = User.FindFirst("UserId")?.Value;
            if (studentIdClaim == null) return Challenge();
            int studentId = int.Parse(studentIdClaim);

            // Kiểm tra xem sinh viên này đã ứng tuyển công việc này chưa để tránh trùng lặp
            var alreadyApplied = await _context.Applications
                .AnyAsync(a => a.JobId == jobId && a.StudentId == studentId);

            if (alreadyApplied)
            {
                TempData["Message"] = "Bạn đã ứng tuyển công việc này rồi!";
                return RedirectToAction("Details", new { id = jobId });
            }

            // Tạo đơn ứng tuyển mới
            var app = new Application
            {
                JobId = jobId,
                StudentId = studentId,
                CoverLetter = coverLetter,
                Status = "Pending",
                AppliedAt = DateTime.Now
            };

            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ứng tuyển thành công! Vui lòng chờ phản hồi từ khách hàng.";
            return RedirectToAction("Details", new { id = jobId });
        }

        // 6. ACTION: Xử lý Khách hàng bấm nút "Duyệt" (Hire) sinh viên làm việc
        [HttpPost]
        [Authorize(Roles = "Client")] // Chỉ khách hàng mới được duyệt
        public async Task<IActionResult> AcceptApplicant(int applicationId)
        {
            // Tìm đơn ứng tuyển
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Job == null) return NotFound();

            // Cập nhật trạng thái đơn ứng tuyển của sinh viên này thành Được nhận
            application.Status = "Accepted";

            // Đổi trạng thái công việc thành Processing (Đang thực hiện)
            application.Job.Status = "Processing";

            // Tự động từ chối tất cả các ứng viên còn lại của công việc này
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
    }
}