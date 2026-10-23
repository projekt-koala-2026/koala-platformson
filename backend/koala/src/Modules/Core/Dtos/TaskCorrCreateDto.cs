namespace koala.src.Modules.Core.Dtos
{
    public record CreateTaskCorrDto
    (
        Guid TaskItemId,
        string CorrectAnswer
    );
}