using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int currentUserId);

            List<Job> chatRooms = new List<Job>();

            if (User.IsInRole("Client"))
            {
                chatRooms = await _context.Jobs
                    .Where(j => j.ClientId == currentUserId)
                    .OrderByDescending(j => j.Id)
                    .ToListAsync();
            }
            else if (User.IsInRole("Student"))
            {
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

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int currentUserIdInt);

            // ĐÃ SỬA: Lấy FullName thực tế của người dùng từ cơ sở dữ liệu
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserIdInt);

            ViewBag.Job = job;
            ViewBag.CurrentUserId = currentUserIdInt;
            ViewBag.CurrentUserName = user?.FullName ?? user?.Email ?? "Ẩn danh";

            return View(messages);
        }

        public async Task<IActionResult> Global()
        {
            var globalMessages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.JobId == null)
                .OrderBy(m => m.SentAt)
                .Take(50)
                .ToListAsync();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int currentUserIdInt);

            // ĐÃ SỬA: Lấy chính xác FullName hiển thị để tránh việc truyền Email làm rách bộ lọc Regex
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserIdInt);

            ViewBag.CurrentUserId = currentUserIdInt;
            ViewBag.CurrentUserName = user?.FullName ?? user?.Email ?? "Thành viên sảnh";

            return View(globalMessages);
        }
    }
}