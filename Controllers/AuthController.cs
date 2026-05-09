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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверка, существует ли пользователь с таким email
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            // Поиск или создание группы
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == registerDto.Group);
            if (group == null)
            {
                group = new Group { GroupName = registerDto.Group };
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
            }

            // Хеширование пароля (простой способ, в реальном проекте используйте BCrypt или Identity)
            var passwordHash = HashPassword(registerDto.Password);

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                LastName = registerDto.LastName,
                FirstName = registerDto.FirstName,
                MiddleName = registerDto.MiddleName,
                Phone = registerDto.Phone,
                GroupID = group.GroupID,
                RoleID = 3, // Студент
                RegistrationDate = DateTime.UtcNow,
                Blocked = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрация успешно завершена", userId = user.UserID });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}