using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class TeamMember
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("member_id")]
        public Guid? MemberId { get; set; }
        [Column("team_id")]
        public Guid TeamId { get; set; }

        public Team Team { get; set; } = null!;
        public Member? Member { get; set; } = null!;
    }
}