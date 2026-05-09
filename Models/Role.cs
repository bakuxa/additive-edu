using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        [Column("roleid")]
        public int RoleID { get; set; }

        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;
    }
}