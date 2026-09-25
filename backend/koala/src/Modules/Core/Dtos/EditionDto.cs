namespace koala.src.Modules.Core.Dtos
{
    public record EditionDto
    (
        Guid Id,
        string Name,
        DateTime CreatedAt,
        DateTime? ExpiredAt
    );
}