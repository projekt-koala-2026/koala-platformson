using koala.src.Modules.Account.Dtos;

namespace koala.src.Shared.Account
{
    public record _SessionResponseDto
    (
        Guid SessionId,
        Guid SessionToken,
        Guid UserId,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        List<string> UserRoles,
        UserDto User
    );

    public record _TeamResponseDto
    (
        Guid TeamId,
        Guid CaptainId,
        Guid GuardianId,
        string Type
    );
}