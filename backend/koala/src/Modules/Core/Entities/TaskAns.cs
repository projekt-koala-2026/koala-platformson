using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskAnswer
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("sent_answer")]
        public string SentAnswer { get; set; }
        public TaskItemTaskAnswer TaskItemTaskAnswer { get; set; } = null!;
    }
}