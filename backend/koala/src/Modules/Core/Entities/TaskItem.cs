using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("content_json", TypeName = "jsonb")]
        public string ContentJson { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public SubEditionTask SubEditionTask { get; set; } = null!;
        public ICollection<TaskItemTaskCorr> TaskItemTaskCorrs { get; set; } = new List<TaskItemTaskCorr>();
        public ICollection<TaskAnswer> TaskAnswers { get; set; } = new List<TaskAnswer>();
    }
}