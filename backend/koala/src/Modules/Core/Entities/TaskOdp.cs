using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskOdp
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("task_id")]
        public Guid TaskId { get; set; }
        [Column("correct_answer")]
        public string CorrectAnswer { get; set; }
        [ForeignKey("TaskId")]
        public TaskItem TaskItem { get; set; }
    }
}