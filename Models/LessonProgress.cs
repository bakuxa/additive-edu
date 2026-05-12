using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("LessonProgress")]
    public class LessonProgress
    {
        [Key]
        [Column("progressid")]
        public int ProgressID { get; set; }

        [Column("user_id")]
        public int UserID { get; set; }

        [Column("lesson_id")]
        public int LessonID { get; set; }

        [Column("progress_percent")]
        public int ProgressPercent { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; }

        [Column("completion_status")]
        public string CompletionStatus { get; set; } = string.Empty;
    }
}