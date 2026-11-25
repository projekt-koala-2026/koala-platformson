using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("Etap1Series_id")]
        public Guid Etap1SeriesId { get; set; }
        [Column("image_id")]
        public Guid? ImageId { get; set; }
        [Column("image_type")]
        public string? ImageType { get; set; }
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
        public Etap1Series Etap1Series { get; set; } = null!;
        public ICollection<TaskAnswer> TaskAnswers { get; set; } = new List<TaskAnswer>();
        public ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();
    }
}
