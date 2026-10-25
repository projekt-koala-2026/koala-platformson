using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public class Member
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name_first")]
        public string NameFirst { get; set; }
        [Column("name_last")]
        public string NameLast { get; set; }
        [Column("position")]
        public string Position { get; set; }
        public TeamMember TeamMember { get; set; } = null!;
    }
}