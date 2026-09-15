namespace koala.src.Modules.Core.Dtos
{
    public record SubeditionDto
    (
        Guid Id,
        Guid EditionId,
        string Name,
        DateTime DataStart,
        DateTime? DataEnd,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}