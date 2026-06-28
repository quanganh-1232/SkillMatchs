using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillMatch.Data;
using SkillMatch.Models;
using System.Collections.Generic;

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
            // 1. Lấy thông tin người dùng hiện tại nếu đã đăng nhập
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserIdInt = 0;
            if (int.TryParse(userIdClaim, out currentUserIdInt))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserIdInt);
                ViewBag.CurrentUserId = currentUserIdInt;
                ViewBag.CurrentUserName = user?.FullName ?? user?.Email ?? "Thành viên sảnh";
            }
            else
            {
                ViewBag.CurrentUserId = 0;
                ViewBag.CurrentUserName = "Ẩn danh";
            }

            // 2. NẠP DỮ LIỆU CHAT SẢNH CHUNG (GLOBAL) - ĐÃ SỬA THEO DATABASE MỚI
            // Sảnh chung là nơi các tin nhắn có ReceiverId == null
            var globalMessages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == null)
                .OrderBy(m => m.SentAt)
                .Take(50)
                .ToListAsync();

            ViewBag.GlobalMessages = globalMessages;

            bool isClient = User.IsInRole("Client");
            ViewBag.IsClientHome = isClient;
            ViewBag.CurrentSearch = searchString;

            if (isClient)
            {
                // ----------------------------------------------------
                // LOGIC CHO CLIENT (XEM DANH SÁCH SINH VIÊN)
                // ----------------------------------------------------
                var studentsQuery = _context.Users.Where(u => u.Role == "Student");

                if (!string.IsNullOrEmpty(searchString))
                {
                    string keyword = searchString.ToLower();
                    studentsQuery = studentsQuery.Where(u =>
                        (u.FullName != null && u.FullName.ToLower().Contains(keyword)) ||
                        (u.Bio != null && u.Bio.ToLower().Contains(keyword)) ||
                        (u.Email != null && u.Email.ToLower().Contains(keyword))
                    );
                }

                var students = await studentsQuery.OrderByDescending(u => u.IsVerified).ToListAsync();
                ViewBag.StudentsList = students;

                // Cột phải hiển thị: Gương mặt xuất sắc (Sinh viên có IsVerified == true)
                ViewBag.FeaturedFaces = await _context.Users
                    .Where(u => u.Role == "Student")
                    .Take(4)
                    .ToListAsync();
            }
            else
            {
                // ----------------------------------------------------
                // LOGIC CHO STUDENT / GUEST (XEM DANH SÁCH JOBS)
                // ----------------------------------------------------
                var jobsQuery = _context.Jobs.Include(j => j.Category).Where(j => j.Status == "Open");

                if (!string.IsNullOrEmpty(searchString))
                {
                    string keyword = searchString.ToLower();
                    jobsQuery = jobsQuery.Where(j =>
                        (j.Title != null && j.Title.ToLower().Contains(keyword)) ||
                        (j.Description != null && j.Description.ToLower().Contains(keyword))
                    );
                }

                var activeJobs = await jobsQuery
                    .OrderByDescending(j => j.IsFeatured)
                    .ThenByDescending(j => j.CreatedAt)
                    .ToListAsync();
                ViewBag.JobsList = activeJobs;

                // XỬ LÝ LỖI GROUP BY TRÊN LINQ EF CORE: Tải danh sách thô về RAM trước (AsEnumerable) sau đó mới GroupBy
                var rawJobsData = await _context.Jobs
                    .Select(j => new { j.ClientId })
                    .ToListAsync();

                var topClientsData = rawJobsData
                    .GroupBy(j => j.ClientId)
                    .Select(g => new { ClientId = g.Key, JobCount = g.Count() })
                    .OrderByDescending(x => x.JobCount)
                    .Take(4)
                    .ToList();

                var clientIds = topClientsData.Select(x => x.ClientId).ToList();
                var rawClients = await _context.Users
                    .Where(u => clientIds.Contains(u.Id))
                    .ToListAsync();

                var topClients = topClientsData
                    .Select(tc => {
                        var client = rawClients.FirstOrDefault(c => c.Id == tc.ClientId);
                        return new TopClientViewModel
                        {
                            FullName = client?.FullName ?? "Đối tác hệ thống",
                            Email = client?.Email ?? "",
                            JobCount = tc.JobCount
                        };
                    })
                    .Where(x => !string.IsNullOrEmpty(x.Email))
                    .ToList();

                ViewBag.TopClients = topClients;
            }

            return View();
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

    public class TopClientViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int JobCount { get; set; }
    }
}