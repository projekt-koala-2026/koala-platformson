using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskAnswer
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("task_item_id")]
        public Guid TaskItemId { get; set; }
        [Column("answer")]
        public string Answer { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        public int Version { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
    }
}