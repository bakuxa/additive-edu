using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Module")]
    public class Module
    {
        [Key]
        [Column("moduleid")]
        public int ModuleID { get; set; }

        [Column("module_title")]
        public string ModuleTitle { get; set; } = string.Empty;

        [Column("module_description")]
        public string? ModuleDescription { get; set; }

        [Column("module_number")]
        public int ModuleNumber { get; set; }

        [Column("difficulty_level")]
        public int DifficultyLevel { get; set; }

        [Column("is_published")]
        public bool IsPublished { get; set; }
    }
}