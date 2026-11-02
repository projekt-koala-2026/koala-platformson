namespace koala.src.Modules.Account.Dtos
{
    public record SessionDto
    (
        Guid SessionId,
        Guid UserId,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime ExpiresAt
    );
}