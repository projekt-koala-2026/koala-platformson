namespace koala.src.Modules.Cms.Dtos
{
    public record UpdateStaticPageRequestDto
    (
        string ContentJson,
        int Version
    );
}