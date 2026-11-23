namespace koala.src.Modules.Account.Dtos
{
    public record AdminTeamDto
    (
        Guid Id,
        Guid EditionId,
        Guid SchoolId,
        string Name,
        string Tier,
        bool IsCensored,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}