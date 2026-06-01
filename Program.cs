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
            builder.Services.AddDbContext<SkillMatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            // 1. C?u hình xác th?c b?ng Cookie
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "SkillMatchAuth";
            })
            .AddCookie("SkillMatchAuth", options =>
            {
                options.LoginPath = "/Account/Login"; // ???ng d?n ??n trang ??ng nh?p n?u ch?a log-in
                options.AccessDeniedPath = "/Account/AccessDenied"; // Trang báo l?i n?u vào nh?m quy?n
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // H?t h?n phiên làm vi?c sau 60 phút
            });

            // Thêm d?ch v? HttpContextAccessor ?? sau này g?i thông tin User ? m?i n?i
            builder.Services.AddHttpContextAccessor();
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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
