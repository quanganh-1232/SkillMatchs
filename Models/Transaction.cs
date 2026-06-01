using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; } // Ai giao dịch
        public int? JobId { get; set; } // Giao dịch cho công việc nào (nếu có)

        [Required]
        public decimal Amount { get; set; }

        // Deposit (Nạp tiền), Withdraw (Rút tiền), Hold (Giữ tiền dự án), Receive (Nhận tiền dự án)
        [Required, MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }
    }
}