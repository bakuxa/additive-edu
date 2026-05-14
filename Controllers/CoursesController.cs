using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/courses/modules
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            var modules = await _context.Modules
                .Where(m => m.IsPublished)
                .OrderBy(m => m.ModuleNumber)
                .Select(m => new
                {
                    m.ModuleID,
                    m.ModuleTitle,
                    m.ModuleDescription,
                    m.ModuleNumber,
                    DifficultyLevel = m.DifficultyLevel,
                    Lessons = _context.Lessons
                        .Where(l => l.ModuleID == m.ModuleID)
                        .OrderBy(l => l.LessonOrder)
                        .Select(l => new
                        {
                            l.LessonID,
                            l.LessonTitle,
                            l.LessonDescription,
                            l.LessonOrder,
                            l.TheoryContent
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(modules);
        }

        // GET: api/courses/module/{moduleId}
        [HttpGet("module/{moduleId}")]
        public async Task<IActionResult> GetModuleById(int moduleId)
        {
            var module = await _context.Modules
                .Where(m => m.ModuleID == moduleId && m.IsPublished)
                .Select(m => new
                {
                    m.ModuleID,
                    m.ModuleTitle,
                    m.ModuleDescription,
                    m.ModuleNumber,
                    DifficultyLevel = m.DifficultyLevel,
                    Lessons = _context.Lessons
                        .Where(l => l.ModuleID == m.ModuleID)
                        .OrderBy(l => l.LessonOrder)
                        .Select(l => new
                        {
                            l.LessonID,
                            l.LessonTitle,
                            l.LessonDescription,
                            l.LessonOrder,
                            l.TheoryContent
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (module == null)
                return NotFound(new { message = "Модуль не найден" });

            return Ok(module);
        }
        // GET: api/courses/user-progress/{userId}
    [HttpGet("user-progress/{userId}")]
    public async Task<IActionResult> GetUserProgress(int userId)
    {
        // Получаем все уроки, которые пользователь завершил
        var completedLessons = await _context.LessonProgresses
            .Where(lp => lp.UserID == userId && lp.IsCompleted == true)
            .Select(lp => lp.LessonID)
            .ToListAsync();
        
        // Получаем все уроки в системе с их модулями
        var allLessons = await _context.Lessons
            .Include(l => l.Module)
            .ToListAsync();
        
        // Группируем по модулям и считаем прогресс
        var modulesProgress = allLessons
            .GroupBy(l => new { l.ModuleID, l.Module.ModuleTitle, l.Module.ModuleNumber })
            .Select(g => new
            {
                ModuleId = g.Key.ModuleID,
                ModuleTitle = g.Key.ModuleTitle,
                ModuleNumber = g.Key.ModuleNumber,
                TotalLessons = g.Count(),
                CompletedLessons = g.Count(l => completedLessons.Contains(l.LessonID)),
                ProgressPercent = g.Count() > 0 
                    ? (int)((double)g.Count(l => completedLessons.Contains(l.LessonID)) / g.Count() * 100)
                    : 0
            })
            .ToList();
        
        return Ok(modulesProgress);
    }
    // GET: api/courses/lesson-tasks/{lessonId}
    [HttpGet("lesson-tasks/{lessonId}")]
    public async Task<IActionResult> GetLessonTasks(int lessonId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.LessonID == lessonId && t.IsActive)
            .Select(t => new
            {
                t.TaskID,
                t.TaskTitle,
                t.TaskDescription,
                t.DifficultyLevel,
                t.MaxScore,
                TypeName = _context.Types
                    .Where(typ => typ.TypeID == t.TypeID)
                    .Select(typ => typ.TypeName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(tasks);
}
    }

}