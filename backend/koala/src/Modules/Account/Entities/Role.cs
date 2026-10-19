using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class Role
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}