namespace koala.src.Modules.Account.Dtos
{
    public record TeamDto
    (
        Guid Id,
        string Name,
        bool IsCensored,
        DateTime CreatedAt,
        List<TeamMemberDto> TeamMembers,
        TeamJoinCodeDto? JoinCode
    );
}