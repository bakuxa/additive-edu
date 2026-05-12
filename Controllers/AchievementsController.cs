using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

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

        // ==================== ДОБАВЛЕННЫЙ МЕТОД ====================
        // DTO для запроса
        public class ClaimXPRequest
        {
            public int UserId { get; set; }
            public int AchievementId { get; set; }
        }

        // DTO для ответа
        public class ClaimXPResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int XpEarned { get; set; }
            public int NewExperience { get; set; }
            public int NewLevel { get; set; }
        }

        [HttpPost("claim-xp")]
        public async Task<IActionResult> ClaimXP([FromBody] ClaimXPRequest request)
        {
            try
            {
                // Проверяем существование пользователя
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new ClaimXPResponse 
                    { 
                        Success = false, 
                        Message = "Пользователь не найден" 
                    });
                }

                // Проверяем существование достижения
                var achievement = await _context.Achievements.FindAsync(request.AchievementId);
                if (achievement == null)
                {
                    return NotFound(new ClaimXPResponse 
                    { 
                        Success = false, 
                        Message = "Достижение не найдено" 
                    });
                }

                // Проверяем, получил ли пользователь это достижение
                var userAchievement = await _context.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserID == request.UserId && ua.AchievementID == request.AchievementId);
                
                if (userAchievement == null)
                {
                    return BadRequest(new ClaimXPResponse 
                    { 
                        Success = false, 
                        Message = "Вы ещё не получили это достижение" 
                    });
                }

                // Находим или создаем запись в Rating
                var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == request.UserId);
                if (rating == null)
                {
                    rating = new Rating
                    {
                        UserID = request.UserId,
                        TotalScore = 0,
                        CurrentLevel = 1,
                        Experience = 0,
                        PositionInRating = 0
                    };
                    _context.Ratings.Add(rating);
                    await _context.SaveChangesAsync();
                    
                    // Перезагружаем чтобы получить Id
                    rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == request.UserId);
                }

                // Начисляем XP
                int xpEarned = achievement.PointsReward;
                rating.Experience += xpEarned;
                rating.TotalScore += xpEarned;
                
                // Обновляем уровень (каждые 100 XP = +1 уровень)
                int newLevel = (rating.Experience / 100) + 1;
                rating.CurrentLevel = newLevel;
                
                await _context.SaveChangesAsync();

                return Ok(new ClaimXPResponse
                {
                    Success = true,
                    Message = $"Вы получили {xpEarned} XP!",
                    XpEarned = xpEarned,
                    NewExperience = rating.Experience,
                    NewLevel = rating.CurrentLevel
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ClaimXPResponse
                {
                    Success = false,
                    Message = $"Ошибка сервера: {ex.Message}"
                });
            }
        }
    }
}