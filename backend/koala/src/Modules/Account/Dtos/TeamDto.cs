namespace koala.src.Modules.Account.Dtos
{
    public record TeamDto
    (
        Guid Id,
        Guid EditionId,
        Guid SchoolId,
        string Name,
        bool IsCensored,
        DateTime CreatedAt,
        List<TeamMemberDto> TeamMembers,
        TeamJoinCodeDto? JoinCode
    );
}