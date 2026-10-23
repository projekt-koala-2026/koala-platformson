namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid SubEditionId,
        string Name,
        dynamic ContentJson,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}