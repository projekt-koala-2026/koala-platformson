using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class TeamMember
    {
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Column("team_id")]
        public Guid TeamId { get; set; }
        [Column("position")]
        public string Position { get; set; }
        public User User { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}