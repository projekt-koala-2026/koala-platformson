using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public static class TaskItemTiers
    {
        public const string TIER_1 = "TIER_1";
        public const string TIER_2 = "TIER_2";
    }
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("subedition_id")]
        public Guid SubEditionId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("tier")]
        public string Tier { get; set; }
        [Column("content_json", TypeName = "jsonb")]
        public string ContentJson { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public SubEdition SubEdition { get; set; } = null!;
        public ICollection<TaskAnswer> TaskAnswers { get; set; } = new List<TaskAnswer>();
        public ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();
    }
}