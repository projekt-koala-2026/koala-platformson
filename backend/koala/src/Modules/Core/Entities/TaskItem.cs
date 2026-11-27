using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItem
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("Etap1Series_id")]
        public Guid Etap1SeriesId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("is_primary_school_task")]
        public bool IsPrimarySchoolTask { get; set; }
        [Column("is_secondary_school_task")]
        public bool IsSecondarySchoolTask { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("version")]
        public int Version { get; set; }
        public Etap1Series Etap1Series { get; set; } = null!;
        public ICollection<TaskPart> TaskParts { get; set; } = new List<TaskPart>();
    }
}
