namespace koala.src.Modules.Core.Dtos
{
    public record TaskDto
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