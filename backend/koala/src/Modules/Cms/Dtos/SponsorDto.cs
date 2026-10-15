namespace koala.src.Modules.Cms.Dtos
{
    public record SponsorDto
    (
        Guid Id,
        string Name,
        string ContentJson,
        bool IsVisible,
        DateTime UpdatedAt,
        DateTime CreatedAt,
        int Version
    );
}
