namespace koala.src.Modules.Account.Dtos
{
    public record TeamMemberQueryDto
    (
        Guid? UserId,
        string? Position
    );
}