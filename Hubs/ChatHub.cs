using Microsoft.AspNetCore.SignalR;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SkillMatch.Data;
using SkillMatch.Models;

namespace SkillMatch.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SkillMatchDbContext _context;
        private const string GlobalGroupName = "global_lounge";

        public ChatHub(SkillMatchDbContext context)
        {
            _context = context;
        }

        public async Task JoinGlobalChat()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroupName);
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroupName);
            await base.OnConnectedAsync();
        }

        public async Task JoinJobChat(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Job_{jobId}");
        }

        public async Task SendMessage(string jobId, string senderId, string senderName, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                // ĐÃ SỬA: Chỉ chuẩn hóa và quét bộ lọc trên chính NỘI DUNG TIN NHẮN (message), tránh quét nhầm tên người dùng
                string normalizedText = message.ToLower()
                    .Replace("không", "0").Replace("khong", "0")
                    .Replace("một", "1").Replace("mot", "1")
                    .Replace("hai", "2").Replace("ba", "3")
                    .Replace("bốn", "4").Replace("bon", "4")
                    .Replace("năm", "5").Replace("nam", "5")
                    .Replace("sáu", "6").Replace("sau", "6")
                    .Replace("bảy", "7").Replace("bay", "7")
                    .Replace("tám", "8").Replace("tam", "8")
                    .Replace("chín", "9").Replace("chin", "9")
                    .Replace(" ", "");

                string phonePattern = @"(?:\+84|0)[. -]?[0-9]{3}[. -]?[0-9]{3}[. -]?[0-9]{3,4}";
                string linkPattern = @"(facebook\.com|fb\.com|zalo\.me|t\.me|telegram\.org|skype|http:\/\/|https:\/\/|www\.)";
                string emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";

                bool isViolated = Regex.IsMatch(normalizedText, phonePattern) ||
                                  Regex.IsMatch(normalizedText, linkPattern, RegexOptions.IgnoreCase) ||
                                  Regex.IsMatch(normalizedText, emailPattern);

                if (isViolated)
                {
                    await Clients.Caller.SendAsync("ReceiveSystemWarning", "Hệ thống bảo mật: Tin nhắn chứa thông tin liên lạc ngoài hệ thống (SĐT, Email, Link).");
                    return;
                }

                if (!int.TryParse(jobId, out int parsedJobId))
                {
                    parsedJobId = 0;
                }
                string timeStr = DateTime.Now.ToString("HH:mm");

                if (!int.TryParse(senderId, out int parsedSenderId)) return;

                var chatMsg = new ChatMessage
                {
                    JobId = parsedJobId == 0 ? null : (int?)parsedJobId,
                    SenderId = parsedSenderId,
                    MessageContent = message,
                    SentAt = DateTime.Now
                };

                _context.ChatMessages.Add(chatMsg);
                await _context.SaveChangesAsync();

                if (parsedJobId == 0)
                {
                    await Clients.Group(GlobalGroupName).SendAsync("ReceiveMessage", senderId.ToString(), senderName, message, timeStr, "global");
                }
                else
                {
                    await Clients.Group($"Job_{jobId}").SendAsync("ReceiveMessage", senderId.ToString(), senderName, message, timeStr, jobId.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SendMessage Hub: " + ex.Message);
            }
        }
    }
}