namespace koala.src.Modules.Account.Dtos
{
    public record SessionDto
    (
        Guid SessionId,
        Guid UserId,
        DateTime CreatedAt,
        DateTime ExpiresAt
    );
}