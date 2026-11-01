using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    //[Table("subeditions")]
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
        public DateTime? AnswerClose { get; set; }
        [Column("state")]
        public string State { get; set; }

        [Column("date_start")]
        public DateTime DateStart { get; set; }

        [Column("date_end")]
        public DateTime? DateEnd { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        public Edition Edition { get; set; } = null!;
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}