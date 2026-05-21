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
            var activities = new List<object>();

            // Новые пользователи
            var newUsers = await _context.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(10)
                .Select(u => new
                {
                    icon = "👤",
                    text = $"Добавлен пользователь: {u.LastName} {u.FirstName}",
                    time = u.RegistrationDate.ToString("dd.MM.yyyy HH:mm"),
                    type = "user_add"
                })
                .ToListAsync();
            activities.AddRange(newUsers);

            // Модули
            var modules = await _context.Modules
                .OrderByDescending(m => m.ModuleID)
                .Take(10)
                .Select(m => new
                {
                    icon = "📚",
                    text = m.IsPublished ? $"Добавлен модуль: {m.ModuleTitle}" : $"Создан черновик модуля: {m.ModuleTitle}",
                    time = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                    type = "module_add"
                })
                .ToListAsync();
            activities.AddRange(modules);

            // Уроки
            var lessons = await _context.Lessons
                .OrderByDescending(l => l.LessonID)
                .Take(10)
                .Select(l => new
                {
                    icon = "📖",
                    text = $"Добавлен урок: {l.LessonTitle}",
                    time = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                    type = "lesson_add"
                })
                .ToListAsync();
            activities.AddRange(lessons);

            // Задания
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.TaskID)
                .Take(10)
                .Select(t => new
                {
                    icon = "📝",
                    text = t.IsActive ? $"Добавлено задание: {t.TaskTitle}" : $"Создано неактивное задание: {t.TaskTitle}",
                    time = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                    type = "task_add"
                })
                .ToListAsync();
            activities.AddRange(tasks);

            // Достижения
            var achievements = await _context.Achievements
                .OrderByDescending(a => a.AchievementID)
                .Take(10)
                .Select(a => new
                {
                    icon = "🏆",
                    text = $"Добавлено достижение: {a.AchievementTitle}",
                    time = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                    type = "achievement_add"
                })
                .ToListAsync();
            activities.AddRange(achievements);

            // Результаты тестов/квестов (выполненные задания)
            var taskResults = await _context.TaskResults
                .Include(tr => tr.User)
                .OrderByDescending(tr => tr.CompletedAt)
                .Where(tr => tr.CompletedAt != null)
                .Take(10)
                .Select(tr => new
                {
                    icon = "✅",
                    text = $"{tr.User.LastName} {tr.User.FirstName} выполнил задание на {tr.Score} баллов",
                    time = tr.CompletedAt != null ? tr.CompletedAt.Value.ToString("dd.MM.yyyy HH:mm") : "",
                    type = "task_complete"
                })
                .ToListAsync();
            activities.AddRange(taskResults);

            // Сортировка по времени и взятие последних 15
            var sortedActivities = activities
                .OrderByDescending(a => a.GetType().GetProperty("time")?.GetValue(a, null))
                .Take(15)
                .ToList();

            return Ok(sortedActivities);
        }

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