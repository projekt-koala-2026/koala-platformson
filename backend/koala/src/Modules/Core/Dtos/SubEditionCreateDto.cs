namespace koala.src.Modules.Core.Dtos
{
    public record CreateSubEditionDto
    (
        string Name,
        DateTime DateStart,
        DateTime? DateEnd
    );
}