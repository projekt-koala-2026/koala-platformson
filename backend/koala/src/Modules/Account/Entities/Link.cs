using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public static class LinkTypes
    {
        public const string TYPE_PASSWORD_RESET = "PASSWORD_RESET";
        public const string TYPE_REGISTRATION = "REGISTRATION";
    }
    public static class LinkStates
    {
        public const string STATE_ACTIVE = "ACTIVE";
        public const string STATE_INACTIVE = "INACTIVE";
    }
    public class Link
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("token")]
        public Guid Token { get; set; }
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
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
        public User User { get; set; } = null!;
    }
}