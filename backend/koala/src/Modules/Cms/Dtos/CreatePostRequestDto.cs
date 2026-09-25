namespace koala.src.Modules.Cms.Dtos
{
    public record CreatePostRequestDto
    (
        Guid EditionId,
        string Name,
        string ContentJson,
        bool IsVisable,
        int Version
    );
}