using Microsoft.AspNetCore.SignalR;
using SkillMatch.Data;
using SkillMatch.Models;
using System;
using System.Threading.Tasks;

namespace SkillMatch.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SkillMatchDbContext _context;

        public ChatHub(SkillMatchDbContext context)
        {
            _context = context;
        }

        // Tham gia vào phòng chat của một công việc cụ thể
        public async Task JoinJobChat(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
        }

        // Gửi tin nhắn tới toàn bộ người trong phòng chat công việc đó
        public async Task SendMessage(string jobId, string senderId, string senderName, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // 1. Lưu tin nhắn vào Database
            var chatMsg = new ChatMessage
            {
                JobId = int.Parse(jobId),
                SenderId = int.Parse(senderId),
                MessageContent = message,
                SentAt = DateTime.Now
            };
            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            // 2. Phát tín hiệu Real-time tới tất cả thành viên trong nhóm Chat của Job này
            await Clients.Group(jobId).SendAsync("ReceiveMessage", senderId, senderName, message, chatMsg.SentAt.ToString("HH:mm"));
        }
    }
}