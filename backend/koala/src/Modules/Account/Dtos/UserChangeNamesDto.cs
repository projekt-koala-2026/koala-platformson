using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record UserChangeNamesDto
    (
        string NameFirst,
        string NameLast,
        int Version
    );

    public class UserChangeNamesDtoValidator : AbstractValidator<UserChangeNamesDto>
    {
        public UserChangeNamesDtoValidator()
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

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}