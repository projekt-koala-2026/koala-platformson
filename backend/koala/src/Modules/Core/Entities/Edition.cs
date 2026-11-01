using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public static class EditionStates
    {
        public static string STATE_ACTIVE = "ACTIVE";
        public static string STATE_INACTIVE = "INACTIVE";
        public static string STATE_CLOSED = "CLOSED";
    }
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
        public ICollection<EditionSubEdition> EditionSubEditions { get; set; } = new List<EditionSubEdition>();
    };
}