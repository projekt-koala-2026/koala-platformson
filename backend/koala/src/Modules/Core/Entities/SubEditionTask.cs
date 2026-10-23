using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class SubEditionTask
    {
        [Column("subedition_id")]
        public int SubEditionId { get; set; }
        [Column("task_id")]
        public int TaskId { get; set; }
        public SubEdition SubEdition { get; set; } = null!;
        public TaskItem TaskItem { get; set; } = null!;
    }
}