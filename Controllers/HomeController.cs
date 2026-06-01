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
        // Khai báo DbContext để tương tác với cơ sở dữ liệu
        private readonly SkillMatchDbContext _context;

        // Tiêm cả Logger và DbContext thông qua hàm khởi tạo (Constructor)
        public HomeController(ILogger<HomeController> logger, SkillMatchDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Sửa hàm Index thành xử lý bất đồng bộ (async Task) để truy vấn DB mượt mà hơn
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Tạo câu truy vấn cơ bản lấy các công việc có trạng thái là "Active"
            var jobsQuery = _context.Jobs.Where(j => j.Status == "Active");

            // 2. Xử lý logic tìm kiếm nếu người dùng có gõ từ khóa ở thanh Search trên Banner
            if (!string.IsNullOrEmpty(searchString))
            {
                jobsQuery = jobsQuery.Where(j => j.Title.Contains(searchString)
                                              || j.Description.Contains(searchString));
            }

            // 3. Sắp xếp dự án mới đăng lên đầu tiên và chuyển thành danh sách (List)
            var activeJobs = await jobsQuery.OrderByDescending(j => j.CreatedAt).ToListAsync();

            // 4. Truyền danh sách công việc sang file Index.cshtml làm Model dữ liệu đầu vào
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