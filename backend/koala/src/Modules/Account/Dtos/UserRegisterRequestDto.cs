using FluentValidation;
using koala.src.Modules.Account.Entities;

namespace koala.src.Modules.Account.Dtos
{
    public record UserRegisterRequestDto
    (
        string Email,
        List<string> UserRoles
    );

    public class UserRegisterRequestDtoValidator : AbstractValidator<UserRegisterRequestDto>
    {
        private static readonly string[] AllowedRoles = new[]
        {
            RoleNames.NAME_ORGANIZATION_ADMIN,
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_REVIEWER,
            RoleNames.NAME_TEAM_CAPTAIN,
            RoleNames.NAME_TEAM_GUARDIAN
        };
        public UserRegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email field cannot be empty!")
                .MaximumLength(320).WithMessage("Email field cannot be larger than 50 characters!")
                .EmailAddress().WithMessage("Email field must be a valid email address!");
            RuleFor(x => x.UserRoles)
                .NotEmpty().WithMessage("Roles field cannot be empty!");
            RuleForEach(x => x.UserRoles)
                .NotEmpty().WithMessage("Role item cannot be empty!")
                .MaximumLength(50).WithMessage("Role item name cannot exceed 50 characters!")
                .Must(role => AllowedRoles.Contains(role))
                .WithMessage("Invalid role specified!")
                .When(x => x.UserRoles != null);
        }
    }
}