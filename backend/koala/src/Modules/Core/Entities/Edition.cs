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
        [Column("winner_tier1_id")]
        public Guid? WinnerTier1Id { get; set; }
        [Column("winner_tier2_id")]
        public Guid? WinnerTier2Id { get; set; }
        [Column("are_winners_public")]
        public bool AreWinnersPublic { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public ICollection<SubEdition> SubEditions { get; set; } = new List<SubEdition>();
        public ICollection<Ranking> Rankings { get; set; } = new List<Ranking>();
        public Winner? WinnerTier1 { get; set; } = null;
        public Winner? WinnerTier2 { get; set; } = null;
    };
}
