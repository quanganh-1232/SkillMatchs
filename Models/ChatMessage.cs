using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        // 1. Người gửi
        [Required]
        public int SenderId { get; set; }

        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; }

        // 2. Người nhận (Để NULL nếu là tin nhắn gửi vào Sảnh chung - Global Lounge)
        public int? ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; }

        // 3. Nội dung tin nhắn
        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
        public string MessageContent { get; set; } = string.Empty;

        // 4. Thời gian gửi
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}