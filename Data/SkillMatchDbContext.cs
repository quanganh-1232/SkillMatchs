using Microsoft.EntityFrameworkCore;
using SkillMatch.Models;

namespace SkillMatch.Data
{
    public class SkillMatchDbContext : DbContext
    {
        public SkillMatchDbContext(DbContextOptions<SkillMatchDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Khóa ngoại từ Job về User (Client) -> Đổi thành Restrict (Không tự động xóa)
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Client)
                .WithMany()
                .HasForeignKey(j => j.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Khóa ngoại từ Application về User (Student) -> Đổi thành Restrict
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Khóa ngoại từ Transaction về User -> Đổi thành Restrict
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Khóa ngoại từ Feedback về Job -> Đổi thành Restrict
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Job)
                .WithMany()
                .HasForeignKey(f => f.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}