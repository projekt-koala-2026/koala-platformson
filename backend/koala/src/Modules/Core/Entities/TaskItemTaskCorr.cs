using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskItemTaskCorr
    {
        [Column("task_item_id")]
        public Guid TaskItemId { get; set; }
        [Column("task_corr_id")]
        public Guid TaskCorrId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;
        public TaskCorr TaskCorr { get; set; } = null!;
    }
}