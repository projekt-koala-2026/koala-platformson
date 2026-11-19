namespace koala.src.Modules.Core.Dtos
{
    public record EditionUpdateDto
    (
        string Name,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose,
        int Version
    );
}