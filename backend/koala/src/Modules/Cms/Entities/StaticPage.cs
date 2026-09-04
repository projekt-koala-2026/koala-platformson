using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Cms.Entities
{
    public class StaticPage
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("path")]
        public string Path { get; set; }
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
    } 
}