using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdditiveEdu.Models
{
    [Table("Rating")]
    public class Rating
    {
        [Key]
        [Column("ratingid")]
        public int RatingID { get; set; }

        [Column("user_id")]
        public int UserID { get; set; }

        [Column("total_score")]
        public int TotalScore { get; set; }

        [Column("current_level")]
        public int CurrentLevel { get; set; }

        [Column("position_in_rating")]
        public int? PositionInRating { get; set; }

        [Column("experience")]
        public int Experience { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}