namespace koala.src.Modules.Core.Dtos
{
    public class CreateTaskItemDto
    {
        public Guid SubEditionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public dynamic? ContentJson { get; set; }
    }
}