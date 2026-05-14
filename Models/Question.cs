using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Question")]
    public class Question
    {
        [Key]
        [Column("questionid")]
        public int QuestionID { get; set; }

        [Column("task_id")]
        public int TaskID { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("question_level")]
        public int QuestionLevel { get; set; }

        [Column("question_order")]
        public int QuestionOrder { get; set; }

        [Column("question_weight")]
        public int QuestionWeight { get; set; }

        [ForeignKey("TaskID")]
        public Task? Task { get; set; }
        
        // Это свойство для связи с ответами
        public ICollection<Answer>? Answers { get; set; }
    }
}