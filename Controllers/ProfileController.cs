using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class UpdateProfileDto
        {
            public int UserId { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? GroupName { get; set; }
            public string? PhotoUrl { get; set; }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserID == updateDto.UserId);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }
            
            if (!string.IsNullOrEmpty(updateDto.Phone))
                user.Phone = updateDto.Phone;
            
            if (!string.IsNullOrEmpty(updateDto.Email))
                user.Email = updateDto.Email;
            
            if (!string.IsNullOrEmpty(updateDto.PhotoUrl))
                user.PhotoUrl = updateDto.PhotoUrl;
            
            if (!string.IsNullOrEmpty(updateDto.GroupName))
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == updateDto.GroupName);
                if (group == null)
                {
                    group = new Group { GroupName = updateDto.GroupName };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }
                user.GroupID = group.GroupID;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Профиль успешно обновлён",
                phone = user.Phone,
                email = user.Email,
                groupName = user.Group?.GroupName ?? "",
                photoUrl = user.PhotoUrl ?? ""
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserID == id);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }
            
            return Ok(new
            {
                userId = user.UserID,
                email = user.Email,
                lastName = user.LastName,
                firstName = user.FirstName,
                middleName = user.MiddleName,
                phone = user.Phone,
                registrationDate = user.RegistrationDate.ToString("dd MMMM yyyy г."),
                groupName = user.Group?.GroupName ?? "",
                photoUrl = user.PhotoUrl ?? "",
                roleId = user.RoleID
            });
        }

        public class UserStatsDto
        {
            public int Experience { get; set; }
            public int Level { get; set; }
            public int CourseProgress { get; set; }
            public int AverageScore { get; set; }
            public int NextLevelExp { get; set; }
            public int CurrentLevelExp { get; set; }
            public int ExperienceToNextLevel { get; set; }
        }

        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserStats(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == userId);
            
            int experience = rating?.Experience ?? 0;
            int level = rating?.CurrentLevel ?? 1;
            
            int nextLevelExp = (level) * 100;
            int currentLevelExp = (level - 1) * 100;
            int experienceToNextLevel = nextLevelExp - experience;
            if (experienceToNextLevel < 0) experienceToNextLevel = 0;
            
            var totalLessons = await _context.Lessons.CountAsync();
            var completedLessons = await _context.LessonProgresses
                .Where(lp => lp.UserID == userId && lp.IsCompleted == true)
                .CountAsync();
            
            int courseProgress = totalLessons > 0 ? (int)((double)completedLessons / totalLessons * 100) : 0;
            
            // ===== РАСКОММЕНТИРОВАНО ДЛЯ СРЕДНЕГО БАЛЛА =====
            var taskResults = await _context.TaskResults
                .Where(tr => tr.UserID == userId)
                .ToListAsync();
            int averageScore = taskResults.Any() ? (int)taskResults.Average(tr => tr.Score) : 0;
            
            return Ok(new UserStatsDto
            {
                Experience = experience,
                Level = level,
                CourseProgress = courseProgress,
                AverageScore = averageScore,
                NextLevelExp = nextLevelExp,
                CurrentLevelExp = currentLevelExp,
                ExperienceToNextLevel = experienceToNextLevel
            });
        }
    }
}