namespace koala.src.Modules.Core.Dtos
{
    public record TaskOdpDto
    (
        Guid Id,
        Guid EditionId,
        Guid SubEditionId,
        Guid TaskId,
        string CorrectAnswer
    );
}