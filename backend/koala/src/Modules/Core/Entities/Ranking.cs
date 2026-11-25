using System.ComponentModel.DataAnnotations.Schema;
using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core
{


    public class Ranking
    {
        [Column("ranking_id")]
        public Guid RankingId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public ICollection<RankingData> RankingDatas { get; set; } = new List<RankingData>();
    };
}
