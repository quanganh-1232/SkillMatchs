using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        public int? JobId { get; set; }

        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        // 2. Định danh ID người gửi (Dùng string để khớp với Identity User Id của ASP.NET)
        [Required]
        public int SenderId { get; set; }
        // Liên kết trực tiếp về lớp User quản lý tài khoản (hỗ trợ cả Client lẫn Student gửi tin)
        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; }

        // 3. Nội dung tin nhắn
        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
        public string MessageContent { get; set; } = string.Empty;

        // 4. Thời gian gửi
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}