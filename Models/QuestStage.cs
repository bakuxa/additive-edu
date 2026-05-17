using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("QuestStage")]
    public class QuestStage
    {
        [Key]
        [Column("queststageid")]
        public int QuestStageID { get; set; }

        [Column("task_id")]
        public int TaskID { get; set; }

        [Column("stage_title")]
        public string StageTitle { get; set; } = string.Empty;

        [Column("stage_description")]
        public string? StageDescription { get; set; }

        [Column("stage_order")]
        public int StageOrder { get; set; }

        [Column("success_condition")]
        public string? SuccessCondition { get; set; }

        [ForeignKey("TaskID")]
        public Task? Task { get; set; }
    }
}