namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid SubEditionId,
        string Name,
        string Tier,
        string ContentJson,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}