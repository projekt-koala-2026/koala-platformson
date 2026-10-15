namespace koala.src.Modules.Account.Dtos
{
    public record ConsumeRegisterLinkRequestDto
    (
        string NameFirst,
        string NameLast,
        string Password,
        bool AcceptedRodo,
        bool AcceptedRules
    );
}