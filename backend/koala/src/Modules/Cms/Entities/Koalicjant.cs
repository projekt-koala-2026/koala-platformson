using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Cms.Entities
{
    public class Koalicjant
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Column("name_first")]
        public string NameFirst { get; set; }
        [Column("name_last")]
        public string NameLast { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("content_json", TypeName = "jsonb")]
        public string ContentJson { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        [Column("is_visiable")]
        public bool IsVisiable { get; set; }
    } 
}