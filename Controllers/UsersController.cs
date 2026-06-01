using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkillMatch.Controllers
{
    public class UsersController : Controller
    {
        private readonly SkillMatchDbContext _context;

        public UsersController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // Action cho phép Client hoặc bất kỳ ai xem Profile chi tiết của Student từ trang Home
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // 1. Lấy thông tin Student từ bảng Users dựa vào id truyền từ trang chủ
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (student == null) return NotFound();

            // 2. GIẢI MÃ JSON TỪ TRƯỜNG student.Skills (Trùng khớp logic bên StudentController)
            StudentProfileData profileData;
            try
            {
                profileData = JsonSerializer.Deserialize<StudentProfileData>(student.Skills ?? "{}") ?? new StudentProfileData();
            }
            catch
            {
                profileData = new StudentProfileData();
            }

            // Đảm bảo dữ liệu không bị null để tránh lỗi lỗi ngoài View của bạn
            profileData.SkillList ??= new List<SkillProgress>();
            profileData.Services ??= new List<StudentService>();
            profileData.Portfolio ??= new List<PortfolioProject>();
            if (string.IsNullOrEmpty(profileData.School))
            {
                profileData.School = "Chưa cập nhật thông tin trường học";
            }

            // 3. Lấy danh sách Feedbacks thực tế của sinh viên này
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Job)
                .Where(f => _context.Applications.Any(a => a.StudentId == id && a.JobId == f.JobId && a.Status == "Accepted"))
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // 4. Đóng gói dữ liệu đẩy qua View y hệt cấu trúc View của bạn yêu cầu
            ViewBag.ProfileData = profileData;
            ViewBag.Feedbacks = feedbacks;
            ViewBag.AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0;

            return View(student);
        }
    }
}