namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record Etap1SeriesDto
    (
        Guid Id,
        Guid EditionId,
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}