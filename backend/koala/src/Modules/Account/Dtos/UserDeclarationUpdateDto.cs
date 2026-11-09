using FluentValidation;
using koala.src.Shared;

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
                .WithErrorCode(KoalaValidationErrorCodes.AgeDeclarationInvalid)
                .WithMessage("You must confirm your age by checking only one age requirement box!");

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}