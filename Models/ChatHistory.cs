using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; } // Người chat với AI

        [Required]
        public string UserMessage { get; set; } = string.Empty; // Câu hỏi của người dùng
        [Required]
        public string BotResponse { get; set; } = string.Empty; // Câu trả lời của AI

        public DateTime ChatAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}