namespace koala.src.Modules.Core.Dtos
{
    public record SubeditionDto
    (
        Guid Id,
        Guid EditionId,
        string Name,
        DateTime DateStart,
        DateTime? DateEnd,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}