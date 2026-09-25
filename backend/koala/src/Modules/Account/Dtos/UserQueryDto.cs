namespace koala.src.Modules.Account.Dtos
{
    public record UserQueryDto
    (
        string? Email = null,
        string? NameFirst = null,
        string? NameLast = null,
        List<string>? UserRoles = null,
        bool? ShowCensord = false
    );
}