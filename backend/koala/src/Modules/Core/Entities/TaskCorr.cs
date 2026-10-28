using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskCorr
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("correct_answer")]
        public string CorrectAnswer { get; set; }
        public TaskItemTaskCorr TaskItemTaskCorr { get; set; } = null!;
    }
}