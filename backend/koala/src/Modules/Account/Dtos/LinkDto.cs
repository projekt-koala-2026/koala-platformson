namespace koala.src.Modules.Account.Dtos
{
    public record LinkDto
    (
        Guid Id,
        string Type,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime ExpiresAt
    );
}