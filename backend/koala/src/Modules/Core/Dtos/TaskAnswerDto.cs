namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record TaskAnswerDto
    (
        Guid Id,
        Guid TaskItemId,
        string Answer,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}