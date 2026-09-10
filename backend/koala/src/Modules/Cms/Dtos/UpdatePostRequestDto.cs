namespace koala.src.Modules.Cms.Dtos
{
    public record UpdatePostRequestDto
    (
        Guid EditionId,
        string Name,
        string ContentJson,
        bool IsVisable,
        int Version
    );
}