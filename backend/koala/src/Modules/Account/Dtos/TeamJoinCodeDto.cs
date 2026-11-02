namespace koala.src.Modules.Account.Dtos
{
    public record TeamJoinCodeDto
    (
        string JoinCode,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime ExpiresAt
    );
}