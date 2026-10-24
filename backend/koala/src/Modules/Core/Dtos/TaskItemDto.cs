namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid SubEditionId,
        string Name,
        string ContentJson,
        DateTime? UpdatedAt
    );
}