namespace koala.src.Modules.Account.Dtos
{
    public record UserDto
    (
        Guid Id,
        string NameFirst,
        string NameLast,
        string Email,
        bool Censored,
        List<string> Roles,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}