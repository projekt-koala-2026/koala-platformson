using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public static class TeamTiers
    {
        public static string TIER_1 = "TIER_1";
        public static string TIER_2 = "TIER_2";
    }
    public class Team
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("edition_id")]
        public Guid EditionId { get; set; }
        [Column("school_id")]
        public Guid SchoolId { get; set; }
        [Column("guardian_id")]
        public Guid? GuardianId { get; set; }
        [Column("captain_id")]
        public Guid CaptainId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("tier")]
        public string Tier { get; set; }
        [Column("name_accepted")]
        public bool NameAccepted { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public TeamJoinCode? TeamJoinCode { get; set; }
        public ICollection<Rodo> Rodos { get; set; } = new List<Rodo>();
        public User Captain { get; set; } = null!;
        public User? Guardian { get; set; } = null;
    }
}
