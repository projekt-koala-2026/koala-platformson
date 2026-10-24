namespace koala.src.Modules.Core.Dtos
{
    public record CreateSubeditionDto
    (
        string Name,
        DateTime DateStart,
        DateTime? DateEnd
    );
}