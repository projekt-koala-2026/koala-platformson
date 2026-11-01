namespace koala.src.Modules.Core.Dtos
{
    public record UpdateTaskAnswerDto
    (
        Guid TaskItemId,
        string Answer
    );
}