using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Cms.Entities
{
    public class Post
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("edition_id")]
        public Guid EditionId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("content_json", TypeName = "jsonb")]
        public string ContentJson { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("is_visable")]
        public bool IsVisable { get; set; }
        [Column("version")]
        public int Version { get; set; }    }
}