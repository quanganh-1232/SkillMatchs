using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; } // Ứng tuyển vào công việc nào

        [Required]
        public int StudentId { get; set; } // Sinh viên nào ứng tuyển

        [Required(ErrorMessage = "Vui lòng nhập lời nhắn giới thiệu bản thân")]
        [Display(Name = "Lời nhắn / Giới thiệu kỹ năng")]
        public string CoverLetter { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending"; // Pending (Chờ duyệt), Accepted (Được nhận), Rejected (Từ chối)

        // Thiết lập mối quan hệ liên kết dữ liệu (Navigation Properties)
        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        [ForeignKey("StudentId")]
        public virtual User? Student { get; set; }
    }
}