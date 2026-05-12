using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("TaskResult")]
    public class TaskResult
    {
        [Key]
        [Column("resultid")]
        public int ResultID { get; set; }

        [Column("user_id")]
        public int UserID { get; set; }

        [Column("task_id")]
        public int TaskID { get; set; }

        [Column("score")]
        public int Score { get; set; }

        [Column("attempt_number")]
        public int AttemptNumber { get; set; }

        [Column("completion_status")]
        public string CompletionStatus { get; set; } = string.Empty;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("TaskID")]
        public Task? Task { get; set; }
    }
}