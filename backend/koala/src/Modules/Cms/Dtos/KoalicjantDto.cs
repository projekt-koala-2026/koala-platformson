namespace koala.src.Modules.Cms.Dtos
{
    public record KoalicjantDto
    (
        Guid Id,
        string NameFirst,
        string NameLast,
        string Email,
        string ContentJson,
        bool IsVisible,
        DateTime UpdatedAt,
        int Version
    );
}
