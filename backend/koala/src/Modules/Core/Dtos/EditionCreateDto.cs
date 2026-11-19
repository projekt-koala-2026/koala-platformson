namespace koala.src.Modules.Core.Dtos
{
    public record EditionCreateDto
    (
        string Name,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose
    );
}