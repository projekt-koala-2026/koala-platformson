namespace koala.src.Modules.Account.Dtos
{
    public record TeamMemberDto
    (
        Guid TeamMemberId,
        Guid TeamId,
        string NameFist,
        string NameLast,
        string RodoState,
        int? RodoVersion,
        DateTime? RodoCreatedAt,
        DateTime? RodoUpdatedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}