using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Core.Entities
{
    public class TaskOdp
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string CorrectAnswer { get; set; }
    }
}