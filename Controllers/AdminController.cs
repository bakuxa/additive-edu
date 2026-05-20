using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;

namespace AdditiveEdu.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Content()
        {
            return View();
        }

        public IActionResult Achievements()
        {
            return View();
        }

        public IActionResult Statistics()
        {
            return View();
        }
        public IActionResult UserView(int id)
        {
            ViewBag.UserId = id;
            return View();
        }
    }
}