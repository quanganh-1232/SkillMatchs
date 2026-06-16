using Microsoft.EntityFrameworkCore;
using SkillMatch.Data;

namespace SkillMatch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Cấu hình Kết nối Cơ sở dữ liệu SQL Server
            builder.Services.AddDbContext<SkillMatchDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 1. Cấu hình xác thực bằng Cookie
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "SkillMatchAuth";
            })
            .AddCookie("SkillMatchAuth", options =>
            {
                options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập nếu chưa log-in
                options.AccessDeniedPath = "/Account/AccessDenied"; // Trang báo lỗi nếu vào nhầm quyền
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Hết hạn phiên làm việc sau 60 phút
            });

            // Thêm dịch vụ HttpContextAccessor để sau này gọi thông tin User ở mọi nơi
            builder.Services.AddHttpContextAccessor();

            // Đăng ký dịch vụ AI Gemini Client an toàn (Tránh Socket Exhaustion)
            builder.Services.AddHttpClient<SkillMatch.Services.GeminiService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // FIX: Bắt buộc phải kích hoạt Authentication trước khi Authorization chạy
            // Định danh xem "Bạn là ai?" trước khi quyết định "Bạn có quyền làm gì?"
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}