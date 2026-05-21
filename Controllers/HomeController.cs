using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }

    public IActionResult Rating()
    {
        return View();
    }

    public IActionResult Achievements()
    {
        return View();
    }

    public IActionResult Courses()
    {
        return View();
    }

    public IActionResult Lesson(int id)
    {
        ViewBag.LessonId = id;
        return View();
    }

    // API для получения курсов на главную страницу
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var modules = await _context.Modules
            .Where(m => m.IsPublished)
            .OrderBy(m => m.ModuleNumber)
            .Select(m => new
            {
                id = m.ModuleID,
                number = m.ModuleNumber,
                title = m.ModuleTitle,
                lessonsCount = _context.Lessons.Count(l => l.ModuleID == m.ModuleID),
                tasksCount = _context.Tasks.Count(t => t.Lesson.ModuleID == m.ModuleID && t.IsActive)
            })
            .ToListAsync();

        return Ok(modules);
    }
    
    [HttpGet("api/courses/module/{moduleId}/lessons")]
    public async Task<IActionResult> GetModuleLessons(int moduleId)
    {
        var lessons = await _context.Lessons
            .Where(l => l.ModuleID == moduleId)
            .OrderBy(l => l.LessonOrder)
            .Select(l => new
            {
                lessonId = l.LessonID,
                lessonTitle = l.LessonTitle,
                lessonOrder = l.LessonOrder
            })
            .ToListAsync();

        return Ok(lessons);
    }
}