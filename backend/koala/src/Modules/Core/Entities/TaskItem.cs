using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }

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

        public SubEditionTask SubEditionTask { get; set; } = null!;
        public ICollection<TaskItemTaskCorr> TaskItemTaskCorrs{ get; set; } = new List<TaskItemTaskCorr>();
    }
}