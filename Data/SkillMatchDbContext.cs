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
    }
}