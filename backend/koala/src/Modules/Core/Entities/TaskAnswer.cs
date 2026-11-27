using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskPartAnswer
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("task_part_id")]
        public Guid TaskPartId { get; set; }
        [Column("answer")]
        public string Answer { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
        public TaskPart TaskPart { get; set; } = null!;
    }
}