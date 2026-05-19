using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;
using System.Security.Cryptography;
using System.Text;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            Console.WriteLine("=== REGISTER CALLED ===");
            Console.WriteLine($"Email: {registerDto?.Email}");
            Console.WriteLine($"LastName: {registerDto?.LastName}");
            Console.WriteLine($"FirstName: {registerDto?.FirstName}");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                var errorMessages = string.Join(", ", errors.Select(e => e.ErrorMessage));
                Console.WriteLine($"ModelState invalid: {errorMessages}");
                return BadRequest(new { message = errorMessages });
            }

            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
                if (existingUser != null)
                {
                    Console.WriteLine("User already exists");
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }

                // Проверяем, существует ли таблица Group
                Console.WriteLine("Checking group...");
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == registerDto.Group);
                if (group == null)
                {
                    Console.WriteLine($"Creating new group: {registerDto.Group}");
                    group = new Group { GroupName = registerDto.Group };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }

                var passwordHash = HashPassword(registerDto.Password);
                Console.WriteLine("Password hashed");

                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    LastName = registerDto.LastName,
                    FirstName = registerDto.FirstName,
                    MiddleName = registerDto.MiddleName ?? "",
                    Phone = registerDto.Phone ?? "",
                    GroupID = group.GroupID,
                    RoleID = 3,
                    RegistrationDate = DateTime.UtcNow,
                    Blocked = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                Console.WriteLine($"User created with ID: {user.UserID}");

                var rating = new Rating
                {
                    UserID = user.UserID,
                    TotalScore = 0,
                    CurrentLevel = 1,
                    Experience = 0
                };
                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();
                Console.WriteLine("Rating created");

                return Ok(new { message = "Регистрация успешно завершена", userId = user.UserID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            
            if (user == null)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var passwordHash = HashPassword(loginDto.Password);
            
            if (user.PasswordHash != passwordHash)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            if (user.Blocked)
            {
                return Unauthorized(new { message = "Ваш аккаунт заблокирован" });
            }

            return Ok(new { 
                message = "Вход выполнен успешно", 
                userId = user.UserID,
                email = user.Email,
                lastName = user.LastName,
                firstName = user.FirstName,
                middleName = user.MiddleName ?? "",
                phone = user.Phone ?? "",
                registrationDate = user.RegistrationDate.ToString("dd MMMM yyyy г."),
                groupName = user.Group?.GroupName ?? "",
                roleId = user.RoleID,
                photoUrl = user.PhotoUrl ?? "",
            });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        public class RegisterDto
        {
            public string LastName { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string Group { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class LoginDto
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }
    }
}