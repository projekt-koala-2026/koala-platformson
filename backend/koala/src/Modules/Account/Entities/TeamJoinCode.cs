using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class TeamJoinCode
    {
        [Column("team_id")]
        public Guid TeamId { get; set; }
        [Column("join_code")]
        public string JoinCode { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
        public Team Team { get; set; } = null!;
    }
}