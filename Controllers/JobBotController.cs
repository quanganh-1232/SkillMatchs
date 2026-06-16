using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using SkillMatch.Services;
using System.Security.Claims;

namespace SkillMatch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới được gọi các API chat này
    public class JobBotController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly SkillMatchDbContext _context;

        public JobBotController(SkillMatchDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        // ==========================================================
        // 1. API LẤY LỊCH SỬ CHAT RIÊNG BIỆT (Front-end gọi khi load trang)
        // ==========================================================
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory()
        {
            try
            {
                // Lấy UserId thật từ hệ thống Identity / Claim Cookie của người dùng hiện tại
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, response = "Bạn chưa đăng nhập hoặc phiên làm việc hết hạn!" });
                }
                int currentUserId = int.Parse(userIdClaim);

                // 🌟 CHỈ bốc dữ liệu có UserId trùng khớp với người đăng nhập
                var history = await _context.ChatHistories
                    .Where(h => h.UserId == currentUserId)
                    .OrderBy(h => h.ChatAt)
                    .Select(h => new
                    {
                        h.Id,
                        h.UserMessage,
                        h.BotResponse,
                        ChatAt = h.ChatAt.ToString("HH:mm dd/MM/yyyy")
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, response = $"Lỗi tải lịch sử: {ex.Message}" });
            }
        }

        // ==========================================================
        // 2. API XỬ LÝ CHAT VÀ LƯU TIN NHẮN THEO USER ID THẬT
        // ==========================================================
        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithBot([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return Ok(new { success = false, response = "Nội dung không được để trống!" });
            }

            try
            {
                // Lấy UserId thật từ Claim Cookie / Token đăng nhập
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, response = "Vui lòng đăng nhập để sử dụng tính năng này!" });
                }
                int currentUserId = int.Parse(userIdClaim);

                // TỐI ƯU LINQ: Lấy danh sách dự án phù hợp với trạng thái hợp lệ
                var dbJobs = await _context.Jobs
                    .Where(j => j.Status == "Active" ||
                                j.Status == "Processing" ||
                                j.Status == "Featured" ||
                                j.Status == "Approved" ||
                                j.Status == "Open" ||
                                j.Status == "open")
                    .Select(j => new
                    {
                        j.Id,
                        j.Title,
                        j.Description,
                        j.Budget,
                        j.Deadline
                    })
                    .ToListAsync();

                // Đóng gói danh sách thành chuỗi thô tường minh cho AI
                string availableJobsRaw = "";
                if (dbJobs != null && dbJobs.Any())
                {
                    var formattedJobs = dbJobs.Select(j =>
                        $"- [Dự án ID: {j.Id}] {j.Title} | Mô tả: {j.Description} | Ngân sách: {j.Budget.ToString("N0")}đ | Hạn chót: {j.Deadline.ToString("dd/MM/yyyy")}");

                    availableJobsRaw = string.Join("\n", formattedJobs);
                }
                else
                {
                    // Cơ chế phòng vệ dữ liệu ảo nếu database trống
                    availableJobsRaw = "- [Dự án ID: 1] Thiết kế giao diện UI/UX cho website công ty xây dựng | Mô tả: Yêu cầu bằng Figma. | Ngân sách: 2,000,000đ | Hạn chót: 25/06/2026\n" +
                                       "- [Dự án ID: 3] Edit video Tiktok ngắn chủ đề công nghệ | Mô tả: Cắt ghép video ngắn bằng Premiere/CapCut. | Ngân sách: 1,000,000đ | Hạn chót: 23/06/2026";
                }

                // Gọi AI Gemini xử lý phản hồi câu hỏi
                string botResponse = await _geminiService.GetJobSuggestionsAsync(request.Message, availableJobsRaw);

                // 🌟 SỬA ĐỔI QUAN TRỌNG: Lưu lịch sử vào bảng ứng với UserId thật của người đăng nhập
                var history = new ChatHistory
                {
                    UserId = currentUserId, // Không còn bị gán cứng = 1 nữa!
                    UserMessage = request.Message,
                    BotResponse = botResponse,
                    ChatAt = DateTime.Now
                };
                _context.ChatHistories.Add(history);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, response = botResponse });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, response = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}