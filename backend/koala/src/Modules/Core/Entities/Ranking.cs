using System.ComponentModel.DataAnnotations.Schema;
using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core
{
    public static class RankingTiers
    {
        public const string TIER_1 = "TIER_1";
        public const string TIER_2 = "TIER_2";
    };

    public class Ranking
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("subedition_id")]
        public Guid SubeditionId { get; set; }
        [Column("team_name")]
        public string TeamName { get; set; }
        [Column("tier")]
        public string Tier { get; set; }
        [Column("points")]
        public int Points { get; set; }
        
        [Column("points_max")]
        public int PointsMax { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public SubEdition SubEdition { get; set; } = null!;
    };
}