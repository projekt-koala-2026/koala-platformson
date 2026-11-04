using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record LinkConsumeResetPasswordRequestDto
    (
        string Password
    );

    public class LinkConsumeResetPasswordRequestDtoValidator : AbstractValidator<LinkConsumeResetPasswordRequestDto>
    {
        public LinkConsumeResetPasswordRequestDtoValidator()
        {
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
        }
    }
}