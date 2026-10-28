namespace koala.src.Modules.Core.Dtos
{
    public record UpdateSubEditionDto
    (
        string Name,
        DateTime DateStart,
        DateTime? DateEnd
    );
}