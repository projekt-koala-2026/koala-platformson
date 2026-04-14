using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Tokens")]
    public class Token
    {
        [Key]
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        
        public string? Value { get; set; } = null;
        public DateTime? CreatedAt { get; set; } = null;
        public DateTime? LastsFor { get; set; } = null;

        
        public User User { get; set; } = null!;
    }
}
