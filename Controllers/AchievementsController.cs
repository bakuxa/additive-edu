using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

namespace AdditiveEdu.Controllers
{
    public class AchievementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AchievementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AchievementsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AchievementsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/achievements/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAchievements(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            // Получаем все достижения
            var allAchievements = await _context.Achievements.ToListAsync();
            
            // Получаем ID достижений, которые есть у пользователя
            var userAchievementIds = await _context.UserAchievements
                .Where(ua => ua.UserID == userId)
                .Select(ua => ua.AchievementID)
                .ToListAsync();

            // Получаем даты получения достижений
            var userAchievementsWithDates = await _context.UserAchievements
                .Where(ua => ua.UserID == userId)
                .ToDictionaryAsync(ua => ua.AchievementID, ua => ua.ReceivedAt);

            // Формируем результат с флагом IsUnlocked
            var result = allAchievements.Select(a => new
            {
                achievementId = a.AchievementID,
                title = a.AchievementTitle,
                description = a.AchievementDescription,
                pointsReward = a.PointsReward,
                conditionDescription = a.ConditionDescription,
                isUnlocked = userAchievementIds.Contains(a.AchievementID),
                unlockedAt = userAchievementIds.Contains(a.AchievementID) 
                    ? userAchievementsWithDates[a.AchievementID].ToString("dd.MM.yyyy")
                    : null
            }).ToList();

            return Ok(new
            {
                userId = userId,
                userName = $"{user.LastName} {user.FirstName}",
                achievements = result,
                totalUnlocked = result.Count(a => a.isUnlocked),
                totalAchievements = result.Count,
                unlockedPercent = result.Count > 0 
                    ? Math.Round((double)result.Count(a => a.isUnlocked) / result.Count * 100, 0)
                    : 0
            });
        }
    }
}