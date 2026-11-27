using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskPart
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("task_item_id")]
        public Guid TaskItemId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("max_submission_length")]
        public int MaxSubmissionLength { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public ICollection<TaskPartAnswer> TaskAnswers { get; set; } = new List<TaskPartAnswer>();
        public ICollection<TaskPartSubmission> TaskSubmissions { get; set; } = new List<TaskPartSubmission>();
    }
}
