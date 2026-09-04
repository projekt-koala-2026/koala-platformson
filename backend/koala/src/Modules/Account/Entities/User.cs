using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{    
    public class User
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name_first")]
        public string? NameFirst { get; set; }
        [Column("name_last")]
        public string? NameLast { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("password_hash")]
        public string? PasswordHash { get; set; }
        [Column("censored")]
        public bool Censored { get; set; }
        [Column("verified")]
        public bool Verified { get; set; }
        [Column("accepted_rodo")]
        public bool AcceptedRodo { get; set; }
        [Column("accepted_rules")]
        public bool AcceptedRules { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("verified_at")]
        public DateTime? VerifiedAt { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Link> Links { get; set; } = new List<Link>();
        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}