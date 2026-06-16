using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatch.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkillMatch.Controllers
{
    [Authorize(Roles = "Client")] // Chỉ Nhà tuyển dụng mới cần thanh toán
    public class PaymentController : Controller
    {
        // Giao diện trang chủ quản lý Ví / Lịch sử thanh toán
        public IActionResult Index()
        {
            // Mô phỏng danh sách lịch sử giao dịch
            var history = new List<Payment>
            {
                new Payment { Id = 1, TransactionCode = "SKM83921", Amount = 2000000, PaymentMethod = "VNPay", Status = PaymentStatus.Success, CreatedAt = DateTime.Now.AddDays(-2), Description = "Thanh toán ngân sách dự án Web App" },
                new Payment { Id = 2, TransactionCode = "SKM91024", Amount = 500000, PaymentMethod = "MoMo", Status = PaymentStatus.Failed, CreatedAt = DateTime.Now.AddDays(-1), Description = "Nạp tiền vào tài khoản SkillMatch" }
            };

            ViewBag.Balance = 2000000; // Số dư mô phỏng hiện tại của Doanh nghiệp
            return View(history);
        }

        // Trang khởi tạo giao dịch nạp tiền
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        // Xử lý nút "Xác nhận thanh toán"
        [HttpPost]
        public async Task<IActionResult> CreatePayment(decimal amount, string paymentMethod)
        {
            if (amount < 10000)
            {
                ModelState.AddModelError("", "Số tiền nạp tối thiểu là 10,000 VND");
                return View("Deposit");
            }

            // 1. Tạo bản ghi giao dịch nháp (Pending) trong hệ thống
            string transactionCode = "SKM" + new Random().Next(100000, 999999);

            // 2. Tích hợp cổng thanh toán thực tế (Ví dụ: Cấu hình URL VNPay/Momo tại đây)
            // Ở đây ta làm luồng mô phỏng: Chuyển hướng đến trang Gateway mô phỏng thành công

            return RedirectToAction("PaymentCallback", new { code = transactionCode, amount = amount, method = paymentMethod, status = "SUCCESS" });
        }

        // Nhận phản hồi từ cổng thanh toán (IPN / Callback Url)
        public IActionResult PaymentCallback(string code, decimal amount, string method, string status)
        {
            var paymentResult = new Payment
            {
                TransactionCode = code,
                Amount = amount,
                PaymentMethod = method,
                CreatedAt = DateTime.Now,
                Status = status == "SUCCESS" ? PaymentStatus.Success : PaymentStatus.Failed,
                Description = $"Nạp tiền qua {method} vào hệ thống SkillMatch."
            };

            // Lưu vào DB ở đây (nếu có DbContext)
            // _context.Payments.Add(paymentResult);
            // _context.SaveChanges();

            return View(paymentResult);
        }
    }
}