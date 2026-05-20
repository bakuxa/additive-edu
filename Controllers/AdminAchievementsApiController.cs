using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/admin/achievements")]  // ИСПРАВЛЕНО: явный маршрут
    public class AdminAchievementsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminAchievementsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAchievements()
        {
            var achievements = await _context.Achievements
                .Select(a => new
                {
                    achievementId = a.AchievementID,
                    achievementTitle = a.AchievementTitle,
                    achievementDescription = a.AchievementDescription,
                    pointsReward = a.PointsReward,
                    conditionDescription = a.ConditionDescription,
                    awardedCount = _context.UserAchievements.Count(ua => ua.AchievementID == a.AchievementID)
                })
                .OrderBy(a => a.achievementId)
                .ToListAsync();
            return Ok(achievements);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalAchievements = await _context.Achievements.CountAsync();
            var totalAwarded = await _context.UserAchievements.CountAsync();
            
            var popular = await _context.Achievements
                .Select(a => new { a.AchievementTitle, Count = _context.UserAchievements.Count(ua => ua.AchievementID == a.AchievementID) })
                .OrderByDescending(a => a.Count)
                .FirstOrDefaultAsync();
                
            var topStudentData = await _context.UserAchievements
                .GroupBy(ua => ua.UserID)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .FirstOrDefaultAsync();
                
            string topStudent = "—";
            if (topStudentData != null)
            {
                var user = await _context.Users.FindAsync(topStudentData.UserId);
                if (user != null)
                {
                    topStudent = $"{user.FirstName} {user.LastName}";
                }
            }
            
            return Ok(new 
            { 
                totalAchievements, 
                totalAwarded, 
                popularAchievement = popular?.AchievementTitle ?? "—", 
                topStudent = topStudent 
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAchievement([FromBody] CreateAchievementDto dto)
        {
            var achievement = new Achievement
            {
                AchievementTitle = dto.Title,
                AchievementDescription = dto.Description ?? "",
                PointsReward = dto.PointsReward,
                ConditionDescription = dto.ConditionDescription ?? ""
            };
            _context.Achievements.Add(achievement);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAchievement(int id, [FromBody] CreateAchievementDto dto)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null) return NotFound();
            achievement.AchievementTitle = dto.Title;
            achievement.AchievementDescription = dto.Description ?? "";
            achievement.PointsReward = dto.PointsReward;
            achievement.ConditionDescription = dto.ConditionDescription ?? "";
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAchievement(int id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null) return NotFound();
            var userAchievements = await _context.UserAchievements.Where(ua => ua.AchievementID == id).ToListAsync();
            _context.UserAchievements.RemoveRange(userAchievements);
            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class CreateAchievementDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int PointsReward { get; set; }
        public string ConditionDescription { get; set; } = "";
    }
}