using System.ComponentModel.DataAnnotations.Schema;
using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core
{
    public class RankingData
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("ranking_id")]
        public Guid RankingId { get; set; }
        [Column("team_name")]
        public string TeamName { get; set; }
        [Column("points_achived")]
        public int PointsAchived { get; set; }
        [Column("points_max")]
        public int PointsMax { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public Ranking Ranking { get; set; } = null!;
    };
}
