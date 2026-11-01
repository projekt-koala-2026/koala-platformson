namespace koala.src.Modules.Core.Dtos
{
    public record CreateEditionDto
    (
        string Name,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose
    );
}