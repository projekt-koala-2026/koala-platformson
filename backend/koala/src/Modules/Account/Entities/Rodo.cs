using System.ComponentModel.DataAnnotations.Schema;
using Mono.TextTemplating;

namespace koala.src.Modules.Account.Entities
{
    public static class RodoTypes
    {
        public static string TYPE_JPG = ".jpg";
        public static string TYPE_PNG = ".png";
        public static string TYPE_PDF = ".pdf";
    }
    public class Rodo
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }

        public TeamMember TeamMember { get; set; } = null!;
    }
}