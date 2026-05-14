using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Answer")]
    public class Answer
    {
        [Key]
        [Column("answerid")]
        public int AnswerID { get; set; }

        [Column("question_id")]
        public int QuestionID { get; set; }

        [Column("answer_text")]
        public string AnswerText { get; set; } = string.Empty;

        [Column("is_correct")]
        public bool IsCorrect { get; set; }

        [ForeignKey("QuestionID")]
        public Question? Question { get; set; }
    }
}