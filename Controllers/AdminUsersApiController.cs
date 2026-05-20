using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;
using System.Security.Cryptography;
using System.Text;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class AdminUsersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminUsersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Group)
                .Select(u => new
                {
                    userId = u.UserID,
                    email = u.Email,
                    lastName = u.LastName,
                    firstName = u.FirstName,
                    middleName = u.MiddleName,
                    phone = u.Phone,
                    registrationDate = u.RegistrationDate,
                    roleId = u.RoleID,
                    blocked = u.Blocked,
                    status = u.Blocked ? "Заблокирован" : "Активен",
                    groupName = u.Group != null ? u.Group.GroupName : "",
                    roleName = u.RoleID == 4 ? "Преподаватель" : "Студент"
                })
                .OrderBy(u => u.userId)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .Where(u => u.UserID == id)
                .Select(u => new
                {
                    userId = u.UserID,
                    email = u.Email,
                    lastName = u.LastName,
                    firstName = u.FirstName,
                    middleName = u.MiddleName,
                    phone = u.Phone,
                    registrationDate = u.RegistrationDate.ToString("dd MMMM yyyy г."),
                    roleId = u.RoleID,
                    blocked = u.Blocked,
                    groupName = u.Group != null ? u.Group.GroupName : "",
                    photoUrl = u.PhotoUrl ?? ""
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == dto.Group);
            if (group == null && !string.IsNullOrEmpty(dto.Group))
            {
                group = new Group { GroupName = dto.Group };
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
            }

            var passwordHash = HashPassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                Phone = dto.Phone,
                GroupID = group?.GroupID,
                RoleID = dto.RoleId,
                RegistrationDate = DateTime.UtcNow,
                Blocked = dto.Blocked
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var rating = new Rating
            {
                UserID = user.UserID,
                TotalScore = 0,
                CurrentLevel = 1,
                Experience = 0
            };
            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, userId = user.UserID });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null && existingUser.UserID != id)
                {
                    return BadRequest(new { message = "Пользователь с таким email уже существует" });
                }
                user.Email = dto.Email;
            }

            user.LastName = dto.LastName;
            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.Phone = dto.Phone;
            user.RoleID = dto.RoleId;
            user.Blocked = dto.Blocked;

            if (!string.IsNullOrEmpty(dto.Group))
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == dto.Group);
                if (group == null)
                {
                    group = new Group { GroupName = dto.Group };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }
                user.GroupID = group.GroupID;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Удаляем связанные данные
            var taskResults = await _context.TaskResults.Where(tr => tr.UserID == id).ToListAsync();
            _context.TaskResults.RemoveRange(taskResults);
            
            var lessonProgress = await _context.LessonProgresses.Where(lp => lp.UserID == id).ToListAsync();
            _context.LessonProgresses.RemoveRange(lessonProgress);
            
            var userAchievements = await _context.UserAchievements.Where(ua => ua.UserID == id).ToListAsync();
            _context.UserAchievements.RemoveRange(userAchievements);
            
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == id);
            if (rating != null)
                _context.Ratings.Remove(rating);
            
            _context.Users.Remove(user);
            
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class CreateUserDto
    {
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Group { get; set; } = "";
        public int RoleId { get; set; }
        public string Password { get; set; } = "";
        public bool Blocked { get; set; } = false;
    }

    public class UpdateUserDto
    {
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Group { get; set; } = "";
        public int RoleId { get; set; }
        public bool Blocked { get; set; } = false;
    }
}