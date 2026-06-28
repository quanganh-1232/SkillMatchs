using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return claim != null ? int.Parse(claim) : 0;
        }

        // =========================================================================
        // ĐÃ THÊM: ACTION ĐÓN TIẾP TỪ NÚT "LIÊN HỆ" Ở HỒ SƠ SINH VIÊN (GIẢI QUYẾT LỖI 404)
        // =========================================================================
        public async Task<IActionResult> Connect(int studentId)
        {
            int myId = GetCurrentUserId();

            // Nếu tự bấm vào nút liên hệ trên hồ sơ của chính mình
            if (myId == studentId) return RedirectToAction(nameof(Index));

            // Kiểm tra sinh viên đó có tồn tại không
            var partnerExists = await _context.Users.AnyAsync(u => u.Id == studentId);
            if (!partnerExists) return NotFound("Không tìm thấy thành viên này.");

            // Điều hướng thẳng về phòng chat Room 1-1, truyền studentId vào tham số jobId của Room
            return RedirectToAction(nameof(Room), new { jobId = studentId });
        }

        // 1. DANH SÁCH CUỘC HỘI THOẠI CÁ NHÂN
        public async Task<IActionResult> Index()
        {
            int myId = GetCurrentUserId();

            // Tìm tất cả ID người dùng mà mình từng nhắn tin qua lại
            var sharedUserIds = await _context.ChatMessages
                .Where(m => m.SenderId == myId || m.ReceiverId == myId)
                .Select(m => m.SenderId == myId ? m.ReceiverId : m.SenderId)
                .Where(id => id != null && id != myId)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync();

            // Lấy thông tin chi tiết của những người đó làm đối tác chat (Partners)
            var partners = await _context.Users
                .Where(u => sharedUserIds.Contains(u.Id))
                .ToListAsync();

            ViewBag.CurrentUserId = myId;
            return View(partners);
        }

        // 2. SẢNH THẢO LUẬN CHUNG SYSTEM
        public async Task<IActionResult> Global()
        {
            int myId = GetCurrentUserId();
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == myId);

            // Lấy 100 tin nhắn sảnh chung (ReceiverId == null)
            var globalMessages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == null)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();

            ViewBag.CurrentUserId = myId;
            ViewBag.CurrentUserName = currentUser?.FullName ?? currentUser?.Email ?? "Thành viên";

            return View(globalMessages);
        }

        // 3. PHÒNG CHAT 1-1 RIÊNG BIỆT
        public async Task<IActionResult> Room(int jobId) // Tham số 'jobId' đóng vai trò là partnerId nhận từ redirect
        {
            int myId = GetCurrentUserId();
            int partnerId = jobId;

            if (myId == partnerId) return RedirectToAction(nameof(Index));

            var partner = await _context.Users.FirstOrDefaultAsync(u => u.Id == partnerId);
            if (partner == null) return NotFound();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == myId);

            // Tải lịch sử tin nhắn giữa 2 người
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == myId && m.ReceiverId == partnerId) ||
                            (m.SenderId == partnerId && m.ReceiverId == myId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.CurrentUserId = myId;
            ViewBag.CurrentUserName = currentUser?.FullName ?? currentUser?.Email ?? "Ẩn danh";
            ViewBag.PartnerId = partnerId;
            ViewBag.PartnerName = partner.FullName ?? partner.Email ?? "Người dùng";

            return View(messages);
        }
    }
}