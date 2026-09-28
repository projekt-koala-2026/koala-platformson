namespace koala.src.Modules.Core.Dtos
{
    public record UpdateSubeditionNameDto
    (
        string Name,
        DateTime DateStart,
        DateTime? DateEnd
    );
}