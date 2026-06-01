using System.ComponentModel.DataAnnotations;

namespace SkillMatch.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả công việc không được để trống")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập ngân sách")]
        [Range(10000, 100000000, ErrorMessage = "Ngân sách từ 10k đến 100 triệu")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn chót")]
        public DateTime Deadline { get; set; }

        public string Status { get; set; } = "Open"; // Open, Processing, Completed, Cancelled

        public int ClientId { get; set; } // ID của Khách hàng đăng tin
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}