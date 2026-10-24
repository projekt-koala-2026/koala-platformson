using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class SubEditionTask
    {
        [Column("subedition_id")]
        public Guid SubEditionId { get; set; }
        [Column("task_id")]
        public Guid TaskId { get; set; }
        public SubEdition SubEdition { get; set; } = null!;
        public TaskItem TaskItem { get; set; } = null!;
    }
}