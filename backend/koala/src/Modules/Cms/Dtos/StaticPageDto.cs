namespace koala.src.Modules.Cms.Dtos
{
    public record StaticPageDto
    (
        Guid Id,
        string Name,
        string ContentJson,
        DateTime UpdatedAt,
        int Version
    );
}