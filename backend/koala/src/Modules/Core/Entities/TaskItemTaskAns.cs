using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItemTaskAnswer
    {
        [Column("task_item_id")]
        public Guid TaskItemId { get; set; }
        [Column("task_answer_id")]
        public Guid TaskAnswerId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public TaskAnswer TaskAnswer { get; set; } = null!;
    }
}