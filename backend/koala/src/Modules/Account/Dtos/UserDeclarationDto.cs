namespace koala.src.Modules.Account.Dtos
{
    public record UserDeclarationDto
    (
        Guid Id,
        bool AcceptedRodo,
        bool AcceptedRules,
        bool DeclaredAtLeast13,
        bool DeclaredAtLeast18
    );
}