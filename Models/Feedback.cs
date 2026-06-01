using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }
        public int JobId { get; set; }
        public int SenderId { get; set; } // Người đánh giá
        public int ReceiverId { get; set; } // Người được đánh giá

        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }
    }
}