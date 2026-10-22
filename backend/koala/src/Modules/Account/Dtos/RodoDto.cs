namespace koala.src.Modules.Account.Dtos
{
    public record RodoDto(
        Guid TeamId,
        Guid? MemberId,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}