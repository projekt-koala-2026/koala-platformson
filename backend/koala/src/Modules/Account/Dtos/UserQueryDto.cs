using FluentValidation;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record UserQueryDto
    (
        string? Email = null,
        string? NameFirst = null,
        string? NameLast = null,
        List<string>? UserRoles = null,
        bool? ShowCensored = false
    );

    public class UserQueryDtoValidator : AbstractValidator<UserQueryDto>
    {
        private static readonly string[] AllowedRoles = new[]
        {
            RoleNames.NAME_ORGANIZATION_ADMIN,
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_REVIEWER,
            RoleNames.NAME_TEAM_CAPTAIN,
            RoleNames.NAME_TEAM_GUARDIAN
        };
        public UserQueryDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.NameFirst))
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstTooLong)
                .WithMessage("NameFirst field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .When(x => !string.IsNullOrEmpty(x.NameFirst))
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstInvalidCharacter)
                .WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.NameLast)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.NameLast))
                .WithErrorCode(KoalaValidationErrorCodes.NameLastTooLong)
                .WithMessage("NameLast field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .When(x => !string.IsNullOrEmpty(x.NameLast))
                .WithErrorCode(KoalaValidationErrorCodes.NameLastInvalidCharacter)
                .WithMessage("NameLast field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.Email)
                .MaximumLength(320)
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithErrorCode(KoalaValidationErrorCodes.EmailTooLong)
                .WithMessage("Email field cannot be larger than 320 characters!")

                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithErrorCode(KoalaValidationErrorCodes.EmailInvalid)
                .WithMessage("Email field must be a valid email address!");

            RuleForEach(x => x.UserRoles)
                .NotEmpty()
                .When(x => x.UserRoles != null)
                .WithErrorCode(KoalaValidationErrorCodes.UserRoleEmpty)
                .WithMessage("Role item cannot be empty!")

                .MaximumLength(50)
                .When(x => x.UserRoles != null)
                .WithErrorCode(KoalaValidationErrorCodes.UserRoleTooLong)
                .WithMessage("Role item name cannot exceed 50 characters!")

                .Must(role => AllowedRoles.Contains(role))
                .When(x => x.UserRoles != null)
                .WithErrorCode(KoalaValidationErrorCodes.UserRoleInvalid)
                .WithMessage("Invalid role specified!");
        }
    }
}
