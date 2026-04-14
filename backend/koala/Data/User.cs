using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
            
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        
        public Token token { get; set; } = null!;
        public ICollection<UserRole> UserRoles {get; set;}
    }
}
