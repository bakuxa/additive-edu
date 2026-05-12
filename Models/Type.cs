using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Type")]
    public class Type
    {
        [Key]
        [Column("typeid")]
        public int TypeID { get; set; }

        [Column("type_name")]
        public string TypeName { get; set; } = string.Empty;
    }
}