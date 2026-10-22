namespace koala.src.Modules.Core.Dtos
{
    public record TaskOdpDto
    (
        Guid Id,
        Guid TaskId,
        string CorrectAnswer
    );
}