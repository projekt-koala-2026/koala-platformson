namespace koala.src.Modules.Core.Dtos
{
    public record TaskCorrDto
    (
        Guid Id,
        Guid TaskId,
        string CorrectAnswer
    );
}