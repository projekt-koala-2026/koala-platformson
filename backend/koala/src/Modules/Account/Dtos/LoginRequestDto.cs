using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record LoginRequestDto
    (
        string Email,
        string Password
    );

    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.EmailEmpty)
                .WithMessage("Email field cannot be empty!")

                .MaximumLength(320)
                .WithErrorCode(KoalaValidationErrorCodes.EmailTooLong)
                .WithMessage("Email field cannot be larger than 320 characters!");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.PasswordEmpty)
                .WithMessage("Password field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.PasswordTooLong)
                .WithMessage("Password field cannot be larger than 50 characters!");
        }
    }
}