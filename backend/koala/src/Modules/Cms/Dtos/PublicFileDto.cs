namespace koala.src.Modules.Cms.Dtos
{
    public record PublicFileDto
    (
        Guid Id,
        string Name,
        string Path,
        string Type,
        DateTime CreatedAt,
        int Version
    );
}