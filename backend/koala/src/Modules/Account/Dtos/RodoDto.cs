namespace koala.src.Modules.Account.Dtos
{
    public record RodoDto(
        Guid TeamId,
        Guid UserId,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}