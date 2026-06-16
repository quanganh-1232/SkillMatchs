using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace SkillMatch.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly SkillMatchDbContext _context;

        public ChatController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // GET: /Chat hoặc /Chat/Index - Danh sách các phòng chat
        public async Task<IActionResult> Index()
        {
            // ĐÃ SỬA: Lấy chính xác Claim "UserId" giống hệt bên JobsController
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            List<Job> chatRooms = new List<Job>();

            if (User.IsInRole("Client"))
            {
                // Nếu là Doanh nghiệp: Lấy các công việc do chính mình đăng
                chatRooms = await _context.Jobs
                    .Where(j => j.ClientId == currentUserId)
                    .OrderByDescending(j => j.Id)
                    .ToListAsync();
            }
            else if (User.IsInRole("Student"))
            {
                // Nếu là Sinh viên: Lấy các công việc mà mình đã ứng tuyển thành công (hoặc đang xử lý)
                chatRooms = await _context.Applications
                    .Where(a => a.StudentId == currentUserId)
                    .Include(a => a.Job)
                    .Select(a => a.Job)
                    .Where(j => j != null)
                    .Distinct()
                    .ToListAsync();
            }

            return View(chatRooms);
        }

        // GET: /Chat/Room?jobId=... - Phòng chat chi tiết
        public async Task<IActionResult> Room(int jobId)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null) return NotFound();

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.JobId == jobId)
                .OrderBy(m => m.SentAt)
                .Take(50)
                .ToListAsync();

            // ĐÃ SỬA: Lấy chính xác Claim "UserId"
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int currentUserIdInt);

            ViewBag.Job = job;
            ViewBag.CurrentUserId = currentUserIdInt;
            ViewBag.CurrentUserName = User.Identity?.Name ?? "Ẩn danh";

            return View(messages);
        }
    }
}