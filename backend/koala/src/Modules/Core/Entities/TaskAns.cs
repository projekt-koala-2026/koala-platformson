using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskAnswer
    {
        
        [Column("team_id")]
        public Guid TeamId { get; set; }
        [Column("task_id")]
        public Guid TaskId { get; set; }
        [Column("reviewer_1_id")]
        public Guid? Reviewer1Id { get; set; }
        [Column("reviewer_2_id")]
        public Guid? Reviewer2Id { get; set; }
        [Column("auto_review_state")]
        public bool? AutoReviewState { get; set; }
        [Column("review_1_state")]
        public bool? Review1State { get; set; }
        [Column("review_2_state")]
        public bool? Review2State { get; set; }

        [Column("team_answer")]
        public string TeamAnswer { get; set; } = string.Empty;
        [Column("version")]
        public int Version { get; set; } = 1;

        public TaskItem TaskItem { get; set; } = null!;
    }
}