namespace koala.src.Modules.Account.Dtos
{
    public record TeamDto
    (
        Guid Id,
        Guid EditionId,
        Guid SchoolId,
        string Name,
        string Tier,
        bool IsCensored,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version,
        UserDto TeamCaptain,
        UserDto? TeamGuardian,
        List<TeamMemberDto> TeamMembers,
        TeamJoinCodeDto? JoinCode
    );
}