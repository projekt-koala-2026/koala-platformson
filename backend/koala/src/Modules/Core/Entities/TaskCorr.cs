using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskCorr
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("task_id")]
        public Guid TaskId { get; set; }
        [Column("correct_answer")]
        public string CorrectAnswer { get; set; }
        public TaskItemTaskCorr TaskItemTaskCorr { get; set; }
    }
}