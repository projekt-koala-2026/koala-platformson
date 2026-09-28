namespace koala.src.Modules.Core.Dtos
{
    public class CreateTaskItemDto
    {
        public Guid EditionId { get; set; }

        public Guid SubeditionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public dynamic? ContentJson { get; set; }

        public DateTime? ExpiredAt { get; set; }
    }
}