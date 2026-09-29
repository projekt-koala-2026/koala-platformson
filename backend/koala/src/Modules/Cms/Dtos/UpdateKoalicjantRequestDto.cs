namespace koala.src.Modules.Cms.Dtos
{
    public record UpdateKoalicjantRequestDto
    (
        string NameFirst,
        string NameLast,
        string Email,
        string ContentJson,
        bool IsVisible,
        int Version
    );
}
