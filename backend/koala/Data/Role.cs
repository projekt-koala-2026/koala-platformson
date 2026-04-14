using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int Id {get; set;}
        public string Value {get; set;}

        public ICollection<UserRole> UserRoles {get; set;}
    }
}
