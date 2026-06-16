using System.ComponentModel.DataAnnotations;

namespace SkillMatch.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student"; // "Student" hoặc "Client"
        public string? Avatar { get; set; }
        public bool IsVerified { get; set; } = false; // Hiện tag "Uy tín"
        public string? Bio { get; set; }

        public string? Skills { get; set; } // Chỉ dành cho Sinh viên (ví dụ: "Edit Video, Canva")
        public decimal Balance { get; set; } = 0; // Số dư tài khoản (cho tính năng ví trung gian)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}