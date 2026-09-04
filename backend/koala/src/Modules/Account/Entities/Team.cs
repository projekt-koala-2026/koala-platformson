using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class Team
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("name_accepted")]
        public bool NameAccepted { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public TeamJoinCode TeamJoinCode { get; set; } = null!;
    }
}