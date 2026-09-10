namespace koala.src.Modules.Cms.Dtos
{
    public record SponsorDto
    (
        Guid Id,
        string Name,
        string ContentJson,
        bool IsVisiable,
        DateTime UpdatedAt,
        DateTime CreatedAt,
        int Version
    );
}