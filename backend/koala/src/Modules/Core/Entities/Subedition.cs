using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public static class SubEditionStates
    {
        public const string STATE_ACTIVE = "ACTIVE";
        public const string STATE_INACTIVE = "INACTIVE";
        public const string STATE_CLOSED = "CLOSED";
    }
    public class SubEdition
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("edition_id")]
        public Guid EditionId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("answer_open")]
        public DateTime AnswerOpen { get; set; }
        [Column("answer_close")]
        public DateTime AnswerClose { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public Edition Edition { get; set; } = null!;
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}