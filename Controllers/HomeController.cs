using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
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
}