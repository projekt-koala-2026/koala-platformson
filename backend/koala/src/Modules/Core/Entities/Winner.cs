using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class Winner
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("team_name")]
        public string TeamName { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public Edition Edition { get; set; } = null!;
    }
}
