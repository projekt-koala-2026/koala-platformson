using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public static class TeamMemberRodoState
    {
        public const string RODOSTATE_ACCEPETED = "RODO_ACCEPTED";
        public const string RODOSTATE_REJECTED = "RODO_REJECTED";
        public const string RODOSTATE_UNVERIFIED = "RODO_UNVERIFIED";
        public const string RODOSTATE_NONE = "RODO_NONE";
    };
    public class TeamMember
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("team_id")]
        public Guid TeamId { get; set; }
        [Column("rodo_id")]
        public Guid? RodoId { get; set; }
        [Column("rodo_state")]
        public string RodoState { get; set; }
        [Column("name_first")]
        public string? NameFirst { get; set; }
        [Column("name_last")]
        public string? NameLast { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public Rodo? Rodo { get; set; } = null;
        public Team Team { get; set; } = null!;
    }
}