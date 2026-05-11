using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/rating/list?groupId={groupId}&page={page}&pageSize={pageSize}
        [HttpGet("list")]
        public async Task<IActionResult> GetRatingList(int? groupId = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Ratings
                .Include(r => r.User)
                    .ThenInclude(u => u.Group)
                .Where(r => r.User.RoleID != 4); // исключаем преподавателей

            // Фильтрация по группе
            if (groupId.HasValue && groupId.Value > 0)
            {
                query = query.Where(r => r.User.GroupID == groupId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var ratings = await query
                .OrderByDescending(r => r.Experience)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    userId = r.UserID,
                    userName = $"{r.User.LastName} {r.User.FirstName}",
                    userGroup = r.User.Group != null ? r.User.Group.GroupName : "",
                    groupId = r.User.GroupID ?? 0,
                    avatarInitials = $"{r.User.FirstName[0]}{r.User.LastName[0]}",
                    level = r.CurrentLevel,
                    experience = r.Experience,
                    achievementsCount = _context.UserAchievements.Count(ua => ua.UserID == r.UserID)
                })
                .ToListAsync();

            return Ok(new { items = ratings, totalPages, currentPage = page, totalCount });
        }

        // GET: api/rating/groups
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _context.Groups
                .Where(g => _context.Users.Any(u => u.GroupID == g.GroupID && u.RoleID != 4))
                .Select(g => new { groupId = g.GroupID, groupName = g.GroupName })
                .ToListAsync();

            return Ok(groups);
        }
    }
}