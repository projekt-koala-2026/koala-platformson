using System.ComponentModel.DataAnnotations.Schema;
using Mono.TextTemplating;

namespace koala.src.Modules.Account.Entities
{
    public class Rodo
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("team_id")]
        public Guid TeamId { get; set; }
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}