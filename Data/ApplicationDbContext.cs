using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Models;

namespace AdditiveEdu.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
       public DbSet<Lesson> Lessons { get; set; }                    // ← добавить
        public DbSet<LessonProgress> LessonProgresses { get; set; }   // ← добавить
        public DbSet<TaskResult> TaskResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Студент" },
                new Role { RoleID = 2, RoleName = "Преподаватель" }
            );
        }
    }
}