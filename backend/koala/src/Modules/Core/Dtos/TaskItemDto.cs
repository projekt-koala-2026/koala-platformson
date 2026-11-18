namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record TaskItemDto
    (
        Guid Id,
        Guid SubEditionId,
        string Name,
        string Tier,
        string ContentJson,
        bool HasImage,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}