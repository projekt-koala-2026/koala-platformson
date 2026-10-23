using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("edition_id")]
        public Guid EditionId { get; set; }

        [Column("subedition_id")]
        public Guid SubEditionId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("content_json")]
        public dynamic ContentJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("expired_at")]
        public DateTime? ExpiredAt { get; set; }

        public SubEdition SubEdition { get; set; } = null!;
        public ICollection<TaskOdp> TaskOdps{ get; set; } = new List<TaskOdp>();
    }
}