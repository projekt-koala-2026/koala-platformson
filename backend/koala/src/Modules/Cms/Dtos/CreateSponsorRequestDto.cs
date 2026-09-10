namespace koala.src.Modules.Cms.Dtos
{
    public record CreateSponsorRequestDto
    (
        string Name,
        string ContentJson,
        bool IsVisable,
        int Version
    );
}