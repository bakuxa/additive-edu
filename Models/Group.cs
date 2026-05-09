using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Group")]
    public class Group
    {
        [Key]
        [Column("groupid")]
        public int GroupID { get; set; }

        [Column("group_name")]
        public string GroupName { get; set; } = string.Empty;
    }
}