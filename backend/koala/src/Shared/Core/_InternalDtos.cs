namespace koala.src.Shared.Core
{
    public record _EditionDto
    (
        Guid Id,
        string Name,
        DateTime CreatedAt,
        DateTime? ExpiresAt
    );
}