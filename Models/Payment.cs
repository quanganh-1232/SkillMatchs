using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillMatch.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TransactionCode { get; set; } // Mã giao dịch (Ví dụ: SKM123456)

        [Required]
        public string ClientId { get; set; } // ID của Nhà tuyển dụng thực hiện giao dịch

        public int? JobId { get; set; } // ID của dự án (nếu là tiền đóng băng cho dự án cụ thể)

        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Số tiền giao dịch

        [Required]
        public string PaymentMethod { get; set; } // VNPay, MoMo, BankTransfer

        public string Description { get; set; } // Nội dung chuyển khoản

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending; // Trạng thái giao dịch
    }

    public enum PaymentStatus
    {
        Pending,   // Đang chờ thanh toán
        Success,   // Thành công
        Failed     // Thất bại
    }
}