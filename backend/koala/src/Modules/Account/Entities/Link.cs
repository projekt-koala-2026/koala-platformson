using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{    
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
        [Column("active")]
        public bool Active { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }
        public User User { get; set; } = null!;
    }
}