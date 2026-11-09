using FluentValidation;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record UserUpdateRolesDto
    (
        List<string> UserRoles,
        int Version
    );

    public class UserUpdateRolesDtoValidator : AbstractValidator<UserUpdateRolesDto>
    {
        private static readonly string[] AllowedRoles = new[]
        {
            RoleNames.NAME_ORGANIZATION_ADMIN,
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_REVIEWER,
            RoleNames.NAME_TEAM_CAPTAIN,
            RoleNames.NAME_TEAM_GUARDIAN
        };
        public UserUpdateRolesDtoValidator()
        {
            RuleFor(x => x.UserRoles)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.UserRolesEmpty)
                .WithMessage("Roles field cannot be empty!");

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

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}