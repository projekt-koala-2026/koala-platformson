using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    //[Table("subeditions")]
    public class Subedition
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("edition_id")]
        public Guid EditionId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("data_start")]
        public DateTime DataStart { get; set; }

        [Column("data_end")]
        public DateTime? DataEnd { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }
    }
}