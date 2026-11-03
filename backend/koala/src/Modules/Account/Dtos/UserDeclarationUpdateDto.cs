using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record UserDeclarationUpdateDto
    (
        bool DeclaredAtLeast13,
        bool DeclaredAtLeast18,
        int Version
    );

    public class UserDeclarationUpdateDtoValidator : AbstractValidator<UserDeclarationUpdateDto>
    {
        public UserDeclarationUpdateDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => x.DeclaredAtLeast13 != x.DeclaredAtLeast18)
                .WithMessage("You must confirm your age by checking at least one age requirement box!");
            RuleFor(x => x.Version)
                .GreaterThan(0).WithMessage("Version field must be greater than 0");
        }
    }
}