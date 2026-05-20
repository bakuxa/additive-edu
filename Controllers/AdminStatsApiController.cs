using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class AdminStatsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminStatsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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
    }
}