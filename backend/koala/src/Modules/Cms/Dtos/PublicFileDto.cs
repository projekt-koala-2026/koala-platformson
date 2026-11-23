namespace koala.src.Modules.Cms.Dtos
{
    public record PublicFileDto
    (
        Guid Id,
        string Name,
        string? Description,
        string Path,
        string Type,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}