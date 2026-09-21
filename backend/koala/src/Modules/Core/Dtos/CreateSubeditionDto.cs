namespace koala.src.Modules.Core.Dtos
{
    public record CreateSubeditionDto
    (
        string Name,
        DateTime DataStart,
        DateTime? DataEnd
    );
}