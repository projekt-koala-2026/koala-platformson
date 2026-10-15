using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Cms.Entities
{
    public class PublicFile
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("path")]
        public string Path { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
    } 
}