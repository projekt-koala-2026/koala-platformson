namespace koala.src.Modules.Core.Dtos
{
    public record TaskOdpDto
    (
        Guid Id,
        Guid EditionId,
        Guid SubeditionId,
        Guid TaskId,
        string CorrectAnswer
    );
}