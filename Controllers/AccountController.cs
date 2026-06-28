using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;
using SkillMatch.Models;
using SkillMatch.Models.ViewModels;
using System.Security.Claims;

namespace SkillMatch.Controllers
{
    public class AccountController : Controller
    {
        private readonly SkillMatchDbContext _context;

        public AccountController(SkillMatchDbContext context)
        {
            _context = context;
        }

        // --- ĐĂNG KÝ ---
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem Email đã tồn tại chưa
                var userExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (userExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng!");
                    return View(model);
                }

                // Tạo User mới (Lưu ý: Dự án thực tế nên mã hóa mật khẩu, ở đây làm thô để bạn dễ hiểu)
                var user = new User
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    Password = model.Password,
                    Role = model.Role,
                    Balance = 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }
            return View(model);
        }

        // --- ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tìm tài khoản trong DB
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    // Thiết lập các thông tin ghi vào Cookie (Claims)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("UserId", user.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "SkillMatchAuth");

                    // Tiến hành Đăng nhập hệ thống bằng Cookie
                    await HttpContext.SignInAsync("SkillMatchAuth", new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác!");
            }
            return View(model);
        }

        // --- ĐĂNG XUẤT ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("SkillMatchAuth");
            return RedirectToAction("Index", "Home");
        }

        // --- TRANG PHÂN QUYỀN SAI ---
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}


