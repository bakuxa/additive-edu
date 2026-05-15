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
    // GET: api/courses/lesson-available/{lessonId}/{userId}
        [HttpGet("lesson-available/{lessonId}/{userId}")]
        public async Task<IActionResult> IsLessonAvailable(int lessonId, int userId)
        {
            // Получаем урок
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId);
            
            if (lesson == null)
                return NotFound(new { available = false, message = "Урок не найден" });
            
            // Получаем все уроки модуля по порядку
            var moduleLessons = await _context.Lessons
                .Where(l => l.ModuleID == lesson.ModuleID)
                .OrderBy(l => l.LessonOrder)
                .ToListAsync();
            
            // Находим индекс текущего урока
            var currentIndex = moduleLessons.FindIndex(l => l.LessonID == lessonId);
            
            // Если это первый урок в модуле - проверяем доступность модуля
            if (currentIndex == 0)
            {
                // Проверяем, пройден ли предыдущий модуль
                var previousModule = await _context.Modules
                    .Where(m => m.ModuleNumber == lesson.Module.ModuleNumber - 1)
                    .FirstOrDefaultAsync();
                
                if (previousModule != null)
                {
                    var previousModuleLessons = await _context.Lessons
                        .Where(l => l.ModuleID == previousModule.ModuleID)
                        .ToListAsync();
                    
                    var completedPreviousModuleLessons = await _context.LessonProgresses
                        .Where(lp => lp.UserID == userId && 
                            previousModuleLessons.Select(l => l.LessonID).Contains(lp.LessonID) &&
                            lp.IsCompleted == true)
                        .CountAsync();
                    
                    // Если предыдущий модуль не пройден полностью - урок недоступен
                    if (completedPreviousModuleLessons < previousModuleLessons.Count)
                    {
                        return Ok(new { available = false, reason = "Сначала завершите предыдущий модуль" });
                    }
                }
                
                return Ok(new { available = true });
            }
            
            // Проверяем, пройден ли предыдущий урок
            var previousLesson = moduleLessons[currentIndex - 1];
            var isPreviousCompleted = await _context.LessonProgresses
                .AnyAsync(lp => lp.UserID == userId && lp.LessonID == previousLesson.LessonID && lp.IsCompleted == true);
            
            if (!isPreviousCompleted)
            {
                return Ok(new { available = false, reason = "Сначала завершите предыдущий урок" });
            }
            
            return Ok(new { available = true });
        }
        // GET: api/courses/user-lessons-progress/{userId}
        [HttpGet("user-lessons-progress/{userId}")]
        public async Task<IActionResult> GetUserLessonsProgress(int userId)
        {
            var lessonsProgress = await _context.LessonProgresses
                .Where(lp => lp.UserID == userId)
                .Select(lp => new
                {
                    lp.LessonID,
                    lp.ProgressPercent,
                    lp.IsCompleted
                })
                .ToListAsync();
            
            return Ok(lessonsProgress);
        }
    }
    

}