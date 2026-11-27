using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class Edition
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("team_register_open")]
        public DateTime TeamRegisterOpen { get; set; }
        [Column("team_register_close")]
        public DateTime TeamRegisterClose { get; set; }

        [Column("tier1_ranking_total_id")]
        public Guid Tier1RankingTotalId { get; set; }
        [Column("tier2_ranking_total_id")]
        public Guid Tier2RankingTotalId { get; set; }
        [Column("are_rankings_public")]
        public bool AreRankingsPublic { get; set; }

        [Column("task_file_open")]
        public DateTime? TaskPdfFileOpen { get; set; }
        [Column("task_file_close")]
        public DateTime? TaskPdfFileClose { get; set; }

        [Column("task_file_id")]
        public Guid? TaskFileId {get; set;}
        [Column("task_file_extension")]
        public string? TaskFileExtension {get; set;}

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public ICollection<Etap1Series> Etap1Series { get; set; } = new List<Etap1Series>();
        public Ranking Tier1RankingTotal { get; set; } = null!;
        public Ranking Tier2RankingTotal { get; set; } = null!;
    };
}
