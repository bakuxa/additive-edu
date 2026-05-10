using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTO для обновления профиля
        public class UpdateProfileDto
        {
            public int UserId { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? GroupName { get; set; }
            public string? PhotoUrl { get; set; }  // ← добавить
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserID == updateDto.UserId);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }
            
            if (!string.IsNullOrEmpty(updateDto.Phone))
                user.Phone = updateDto.Phone;
            
            if (!string.IsNullOrEmpty(updateDto.Email))
                user.Email = updateDto.Email;
            
            if (!string.IsNullOrEmpty(updateDto.PhotoUrl))
                user.PhotoUrl = updateDto.PhotoUrl;
            
            if (!string.IsNullOrEmpty(updateDto.GroupName))
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == updateDto.GroupName);
                if (group == null)
                {
                    group = new Group { GroupName = updateDto.GroupName };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }
                user.GroupID = group.GroupID;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Профиль успешно обновлён",
                phone = user.Phone,
                email = user.Email,
                groupName = user.Group?.GroupName ?? "",
                photoUrl = user.PhotoUrl ?? ""
            });
        }
        // Получение данных пользователя по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.UserID == id);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }
            
            return Ok(new
            {
                userId = user.UserID,
                email = user.Email,
                lastName = user.LastName,
                firstName = user.FirstName,
                middleName = user.MiddleName,
                phone = user.Phone,
                registrationDate = user.RegistrationDate.ToString("dd MMMM yyyy г."),
                groupName = user.Group?.GroupName ?? "",
                photoUrl = user.PhotoUrl ?? "",
                roleId = user.RoleID
            });
        }
    }
}