using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var usersCount = await _context.Users.CountAsync();
            var modulesCount = await _context.Modules.CountAsync();
            var lessonsCount = await _context.Lessons.CountAsync();
            var tasksCount = await _context.Tasks.CountAsync();
            var achievementsCount = await _context.Achievements.CountAsync();

            return Ok(new
            {
                usersCount,
                modulesCount,
                lessonsCount,
                tasksCount,
                achievementsCount
            });
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .Select(u => new
                {
                    icon = "👤",
                    text = $"Новый пользователь: {u.LastName} {u.FirstName}",
                    time = u.RegistrationDate.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            var recentResults = await _context.TaskResults
                .OrderByDescending(tr => tr.CompletedAt)
                .Take(5)
                .Select(tr => new
                {
                    icon = "✅",
                    text = $"Задание выполнено на {tr.Score} баллов",
                    time = tr.CompletedAt != null ? tr.CompletedAt.Value.ToString("dd.MM.yyyy HH:mm") : ""
                })
                .ToListAsync();

            var allActivities = recentUsers.Concat(recentResults)
                .OrderByDescending(a => a.time)
                .Take(5)
                .ToList();

            return Ok(allActivities);
        }

        // ДОБАВЬТЕ ЭТОТ МЕТОД
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => !u.Blocked);
                var teachersCount = await _context.Users.CountAsync(u => u.RoleID == 4);
                var studentsCount = await _context.Users.CountAsync(u => u.RoleID == 3);
                
                var registrationsByMonth = new List<object>();
                var now = DateTime.UtcNow;
                
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);
                    var count = await _context.Users.CountAsync(u => u.RegistrationDate >= monthStart && u.RegistrationDate < monthEnd);
                    registrationsByMonth.Add(new
                    {
                        month = monthStart.ToString("MMM yyyy"),
                        count = count
                    });
                }
                
                var groupsPerformance = new List<object>();
                var groups = await _context.Groups.ToListAsync();
                
                foreach (var group in groups)
                {
                    var students = await _context.Users
                        .Where(u => u.GroupID == group.GroupID && u.RoleID == 3)
                        .ToListAsync();
                    
                    if (!students.Any()) continue;
                    
                    var studentIds = students.Select(s => s.UserID).ToList();
                    
                    var taskResults = await _context.TaskResults
                        .Where(tr => studentIds.Contains(tr.UserID))
                        .ToListAsync();
                    
                    var averageScore = taskResults.Any() ? (int)taskResults.Average(tr => tr.Score) : 0;
                    
                    var completedLessons = await _context.LessonProgresses
                        .CountAsync(lp => studentIds.Contains(lp.UserID) && lp.IsCompleted);
                    var totalLessons = await _context.Lessons.CountAsync();
                    var progressPercent = totalLessons > 0 ? (completedLessons * 100) / (students.Count * totalLessons) : 0;
                    
                    groupsPerformance.Add(new
                    {
                        groupName = group.GroupName ?? "Без группы",
                        studentCount = students.Count,
                        averageScore = averageScore,
                        progressPercent = progressPercent
                    });
                }
                
                var topStudents = await _context.Ratings
                    .Include(r => r.User)
                    .ThenInclude(u => u.Group)
                    .Where(r => r.User.RoleID == 3 && !r.User.Blocked)
                    .OrderByDescending(r => r.Experience)
                    .Take(10)
                    .Select(r => new
                    {
                        userId = r.UserID,
                        firstName = r.User.FirstName ?? "",
                        lastName = r.User.LastName ?? "",
                        groupName = r.User.Group != null ? r.User.Group.GroupName : "",
                        level = r.CurrentLevel,
                        experience = r.Experience
                    })
                    .ToListAsync();
                
                return Ok(new
                {
                    totalUsers,
                    activeUsers,
                    teachersCount,
                    studentsCount,
                    registrationsByMonth,
                    groupsPerformance,
                    topStudents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}