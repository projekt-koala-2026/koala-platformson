namespace koala.src.Modules.Core.Dtos
{
    public record UpdateSubEditionNameDto
    (
        string Name,
        DateTime DateStart,
        DateTime? DateEnd
    );
}