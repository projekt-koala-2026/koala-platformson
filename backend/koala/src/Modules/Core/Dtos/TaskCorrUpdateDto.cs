namespace koala.src.Modules.Core.Dtos
{
    public record UpdateTaskCorrDto
    (
        Guid TaskItemId,
        string CorrectAnswer
    );
}