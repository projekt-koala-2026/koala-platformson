using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class Edition
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("team_register_open")]
        public DateTime TeamRegisterOpen { get; set; }
        [Column("team_register_close")]
        public DateTime TeamRegisterClose { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public ICollection<Etap1Series> Etap1Series { get; set; } = new List<Etap1Series>();
        public ICollection<Ranking> Rankings { get; set; } = new List<Ranking>();
    };
}
