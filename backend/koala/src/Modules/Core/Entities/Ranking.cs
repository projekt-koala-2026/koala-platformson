using System.ComponentModel.DataAnnotations.Schema;
using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core
{


    public class Ranking
    {
        [Column("ranking_id")]
        public Guid RankingId { get; set; }
        [Column("edition_id")]
        public Guid EditionId { get; set; }
        [Column("is_public")]
        public bool IsPublic { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("tier")]
        public string Tier { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public ICollection<RankingData> RankingDatas { get; set; } = new List<RankingData>();
        public Edition Edition { get; set; } = null!;
    };
}
