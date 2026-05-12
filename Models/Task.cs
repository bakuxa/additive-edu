using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Task")]
    public class Task
    {
        [Key]
        [Column("taskid")]
        public int TaskID { get; set; }

        [Column("lesson_id")]
        public int LessonID { get; set; }

        [Column("type_id")]
        public int TypeID { get; set; }

        [Column("task_title")]
        public string TaskTitle { get; set; } = string.Empty;

        [Column("task_description")]
        public string? TaskDescription { get; set; }

        [Column("difficulty_level")]
        public int DifficultyLevel { get; set; }

        [Column("max_score")]
        public int MaxScore { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [ForeignKey("LessonID")]
        public Lesson? Lesson { get; set; }

        [ForeignKey("TypeID")]
        public Type? Type { get; set; }
    }
}