using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core
{
    public class School
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name_full")]
        public string NameFull { get; set; }
        [Column("name_short")]
        public string NameShort { get; set; }
        [Column("rspo")]
        public string Rspo { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("city")]
        public string City { get; set; }
        [Column("road")]
        public string Road { get; set; }
        [Column("building")]
        public string Building { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    };
}