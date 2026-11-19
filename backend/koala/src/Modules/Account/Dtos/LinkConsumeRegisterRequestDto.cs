using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record LinkConsumeRegisterRequestDto
    (
        string NameFirst,
        string NameLast,
        string Password,
        bool AcceptedRodo,
        bool AcceptedRules,
        bool DeclaredAtLeast13,
        bool DeclaredAtLeast18
    );

    public class LinkConsumeRegisterRequestDtoValidator : AbstractValidator<LinkConsumeRegisterRequestDto>
    {
        public LinkConsumeRegisterRequestDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstEmpty)
                .WithMessage("NameFirst field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstTooLong)
                .WithMessage("NameFirst field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstInvalidCharacter)
                .WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.NameLast)

                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.NameLastEmpty)
                .WithMessage("NameLast field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.NameLastTooLong)
                .WithMessage("NameLast field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.NameLastInvalidCharacter)
                .WithMessage("NameLast field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.PasswordEmpty)
                .WithMessage("Password field cannot be empty!")

                .MinimumLength(8)
                .WithErrorCode(KoalaValidationErrorCodes.PasswordTooShort)
                .WithMessage("Password field must be at least 8 characters long!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.PasswordTooLong)
                .WithMessage("Password field cannot be larger than 50 characters!")

                .Matches("[A-Z]")
                .WithErrorCode(KoalaValidationErrorCodes.PasswordLacksCapitalCharacter)
                .WithMessage("Password field must contain at least one uppercase letter!")

                .Matches("[a-z]")
                .WithErrorCode(KoalaValidationErrorCodes.PasswordLacksSmallCharacter)
                .WithMessage("Password field must contain at least one lowercase letter!")

                .Matches("[0-9]")
                .WithErrorCode(KoalaValidationErrorCodes.PasswordLacksNumberCharacter)
                .WithMessage("Password field must contain at least one number!")

                .Matches("[^a-zA-Z0-9]")
                .WithErrorCode(KoalaValidationErrorCodes.PasswordLacksSpecialCharacter)
                .WithMessage("Password field must contain at least one special character!");

            RuleFor(x => x.AcceptedRodo)
                .Equal(true)
                .WithErrorCode(KoalaValidationErrorCodes.RodoNotAccepted)
                .WithMessage("AcceptedRodo field must be true!");

            RuleFor(x => x.AcceptedRules)
                .Equal(true)
                .WithErrorCode(KoalaValidationErrorCodes.RulesNotAccepted)
                .WithMessage("AcceptedRules field must be true!");

            RuleFor(x => x)
                .Must(x => x.DeclaredAtLeast13 != x.DeclaredAtLeast18)
                .WithErrorCode(KoalaValidationErrorCodes.AgeDeclarationInvalid)
                .WithMessage("You must confirm your age by checking only one age requirement box!");
        }
    }
}