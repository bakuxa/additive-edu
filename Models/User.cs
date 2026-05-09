using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("User")]  
    public class User
    {
        [Key]
        [Column("userid")]
        public int UserID { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Column("middle_name")]
        public string? MiddleName { get; set; }

        [Column("role_id")]
        public int RoleID { get; set; }

        [Column("registration_date")]
        public DateTime RegistrationDate { get; set; }

        [Column("blocked")]
        public bool Blocked { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }

        [Column("group_id")]
        public int? GroupID { get; set; }

        [ForeignKey("RoleID")]
        public Role? Role { get; set; }

        [ForeignKey("GroupID")]
        public Group? Group { get; set; }
    }
}