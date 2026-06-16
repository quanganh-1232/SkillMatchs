using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public decimal Budget { get; set; }
        [Required]
        public DateTime Deadline { get; set; }

        public string? Thumbnail { get; set; }
        public string? DeliveryTime { get; set; } // Ví dụ: "2-3 ngày"
        public bool IsFeatured { get; set; } = false; // Mặc định là false
        public int ViewsCount { get; set; } = 0;
        public string Status { get; set; } = "Open";

        public int ClientId { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClientId")]
        public virtual User? Client { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}