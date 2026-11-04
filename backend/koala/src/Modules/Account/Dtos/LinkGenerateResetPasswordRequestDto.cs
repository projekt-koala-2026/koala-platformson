using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record LinkGenerateResetPasswordRequestDto
    (
        string Email
    );

    public class LinkGenerateResetPasswordRequestDtoValidator : AbstractValidator<LinkGenerateResetPasswordRequestDto>
    {
        public LinkGenerateResetPasswordRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.EmailEmpty)
                .WithMessage("Email field cannot be empty!")

                .MaximumLength(320)
                .WithErrorCode(KoalaValidationErrorCodes.EmailTooLong)
                .WithMessage("Email field cannot be larger than 320 characters!")

                .EmailAddress()
                .WithErrorCode(KoalaValidationErrorCodes.EmailInvalid)
                .WithMessage("Email field must be a valid email address!");
        }

    }
}