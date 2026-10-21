namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid EditionId,
        Guid SubEditionId,
        string Name,
        dynamic ContentJson,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}