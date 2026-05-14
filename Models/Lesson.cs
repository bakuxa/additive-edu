using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Lesson")]
    public class Lesson
    {
        [Key]
        [Column("lessonid")]
        public int LessonID { get; set; }

        [Column("module_id")]
        public int ModuleID { get; set; }

        [Column("lesson_title")]
        public string LessonTitle { get; set; } = string.Empty;

        [Column("lesson_description")]
        public string? LessonDescription { get; set; }

        [Column("lesson_order")]
        public int LessonOrder { get; set; }

        [Column("theory_content")]
        public string? TheoryContent { get; set; }

        // Добавьте это навигационное свойство
        [ForeignKey("ModuleID")]
        public virtual Module? Module { get; set; }
    }
}