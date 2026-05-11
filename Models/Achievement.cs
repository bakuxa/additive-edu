using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Achievement")]
    public class Achievement
    {
        [Key]
        [Column("achievementid")]
        public int AchievementID { get; set; }

        [Column("achievement_title")]
        public string AchievementTitle { get; set; } = string.Empty;

        [Column("achievement_description")]
        public string? AchievementDescription { get; set; }

        [Column("points_reward")]
        public int PointsReward { get; set; }

        [Column("condition_description")]
        public string? ConditionDescription { get; set; }
    }
}