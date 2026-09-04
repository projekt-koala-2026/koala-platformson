namespace koala.src.Modules.Account.Dtos
{
    public record LinkDto
    (
        Guid Id,
        string Type,
        DateTime CreatedAt,
        DateTime ExpiresAt
    );
}