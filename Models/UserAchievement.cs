using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("UserAchievement")]
    public class UserAchievement
    {
        [Key]
        [Column("userachievementid")]
        public int UserAchievementID { get; set; }

        [Column("user_id")]
        public int UserID { get; set; }

        [Column("achievement_id")]
        public int AchievementID { get; set; }

        [Column("received_at")]
        public DateTime ReceivedAt { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("AchievementID")]
        public Achievement? Achievement { get; set; }
    }
}