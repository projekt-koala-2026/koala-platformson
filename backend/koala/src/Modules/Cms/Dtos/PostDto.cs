namespace koala.src.Modules.Cms.Dtos
{
    public record PostDto
    (
        Guid Id,
        Guid EditionId,
        string Name,
        string ContentJson,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsVisable,
        int Version
    );
}