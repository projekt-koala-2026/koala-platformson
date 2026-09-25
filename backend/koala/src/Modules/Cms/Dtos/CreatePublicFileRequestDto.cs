namespace koala.src.Modules.Cms.Dtos
{
    public record CreatePublicFileRequestDto
    (
        string Name,
        IFormFile File
    );
}