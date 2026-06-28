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

        // Vào sảnh chung
        public async Task JoinGlobalChat()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroupName);
        }

        // Vào phòng chat 1-1 cá nhân (Sinh ra Group chung duy nhất giữa 2 Id)
        public async Task JoinPrivateChat(string currentUserId, string partnerId)
        {
            if (int.TryParse(currentUserId, out int id1) && int.TryParse(partnerId, out int id2))
            {
                string groupName = $"chat_private_{Math.Min(id1, id2)}_{Math.Max(id1, id2)}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }
        }

        public async Task SendMessage(string receiverIdStr, string senderIdStr, string senderName, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // --- Logic bảo mật chặn SĐT / Email / Link ---
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

            if (Regex.IsMatch(normalizedText, phonePattern) ||
                Regex.IsMatch(normalizedText, linkPattern, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(normalizedText, emailPattern))
            {
                await Clients.Caller.SendAsync("ReceiveSystemWarning", "Hệ thống bảo mật: Tin nhắn chứa thông tin liên lạc ngoài hệ thống (SĐT, Email, Link).");
                return;
            }

            if (!int.TryParse(senderIdStr, out int senderId)) return;
            int? receiverId = null;
            if (!string.IsNullOrEmpty(receiverIdStr) && receiverIdStr.ToLower() != "global" && receiverIdStr != "0")
            {
                if (int.TryParse(receiverIdStr, out int parsedId)) receiverId = parsedId;
            }

            // Lưu vào Database
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageContent = message,
                SentAt = DateTime.Now
            };
            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            string timeStr = chatMsg.SentAt.ToString("HH:mm");

            // Đẩy Realtime theo kênh tương ứng
            if (receiverId == null)
            {
                // Gửi ra sảnh chung
                await Clients.Group(GlobalGroupName).SendAsync("ReceiveMessage", senderIdStr, senderName, message, timeStr, "global");
            }
            else
            {
                // Gửi vào phòng 1-1 mã hóa tên nhóm
                string groupName = $"chat_private_{Math.Min(senderId, receiverId.Value)}_{Math.Max(senderId, receiverId.Value)}";
                await Clients.Group(groupName).SendAsync("ReceiveMessage", senderIdStr, senderName, message, timeStr, receiverIdStr);
            }
        }
    }
}