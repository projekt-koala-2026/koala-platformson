namespace koala.src.Modules.Account.Dtos
{
    public record RegisterUserRequestDto
    (
        string Email,
        List<string> Roles
    );
}