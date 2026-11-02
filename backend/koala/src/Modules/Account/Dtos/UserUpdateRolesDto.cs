using FluentValidation;
using koala.src.Modules.Account.Entities;

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
                .NotEmpty().WithMessage("Roles field cannot be empty!");
            RuleForEach(x => x.UserRoles)
                .NotEmpty().WithMessage("Role item cannot be empty!")
                .MaximumLength(50).WithMessage("Role item name cannot exceed 50 characters!")
                .Must(role => AllowedRoles.Contains(role))
                .WithMessage("Invalid role specified!")
                .When(x => x.UserRoles != null);

            RuleFor(x => x.Version)
                .GreaterThan(0).WithMessage("Version field must be greater than 0");
        }
    }
}