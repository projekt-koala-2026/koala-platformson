namespace koala.src.Modules.Cms.Dtos
{
    public record UpdateSponsorRequestDto
    (
        string Name,
        string ContentJson,
        bool IsVisible,
        int Version
    );
}
