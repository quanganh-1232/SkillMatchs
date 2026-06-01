using System.ComponentModel.DataAnnotations;

namespace SkillMatch.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Ví dụ: Video Editing, Slide Making
        public string? Description { get; set; }
    }
}