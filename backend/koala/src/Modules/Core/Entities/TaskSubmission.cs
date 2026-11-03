using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public static class TaskSubmissionStates
    {
        public const string STATE_UNCHECKED = "UN_CHECKED";
    }
    public class TaskSubmission
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
        public bool? Review1Score { get; set; }
        [Column("review_2_state")]
        public bool? Review2Score { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("submission")]
        public string Submission { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }

        public TaskItem TaskItem { get; set; } = null!;
    }
}