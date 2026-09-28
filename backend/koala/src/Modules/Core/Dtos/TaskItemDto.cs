namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid EditionId,
        Guid SubeditionId,
        string Name,
        dynamic ContentJson,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}