using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillMatch.Data;
using SkillMatch.Models;

namespace SkillMatch.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SkillMatchDbContext _context;

        public HomeController(ILogger<HomeController> logger, SkillMatchDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Sửa điều kiện Status từ "Active" thành "Open" để khớp với Database mẫu công việc đang mở tuyển
            var jobsQuery = _context.Jobs.Include(j => j.Category).Where(j => j.Status == "Open");

            // 2. Logic tìm kiếm theo từ khóa (giữ nguyên)
            if (!string.IsNullOrEmpty(searchString))
            {
                jobsQuery = jobsQuery.Where(j => j.Title.Contains(searchString)
                                              || j.Description.Contains(searchString));
            }

            // 3. Sắp xếp: Ưu tiên công việc NỔI BẬT (IsFeatured) lên trước
            var activeJobs = await jobsQuery
                .OrderByDescending(j => j.IsFeatured)
                .ThenByDescending(j => j.CreatedAt)
                .ToListAsync();

            // 4. BỔ SUNG: Kéo danh sách Sinh viên tiêu biểu (IsVerified == true) từ Database lên trang chủ
            var topStudents = await _context.Users
                .Where(u => u.Role == "Student" && u.IsVerified == true)
                .Take(4) // Lấy tối đa 4 sinh viên xuất sắc nhất hiển thị sidebar
                .ToListAsync();

            // Đẩy danh sách sinh viên vào ViewBag để file Index.cshtml bóc tách ra
            ViewBag.TopStudents = topStudents;

            // Trả danh sách Job về làm Model chính của trang chủ
            return View(activeJobs);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}