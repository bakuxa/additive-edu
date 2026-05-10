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

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == registerDto.Group);
            if (group == null)
            {
                group = new Group { GroupName = registerDto.Group };
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
            }

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
                RoleID = 3,
                RegistrationDate = DateTime.UtcNow,
                Blocked = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрация успешно завершена", userId = user.UserID });
        }

       [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Подгружаем группу
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
    }
}