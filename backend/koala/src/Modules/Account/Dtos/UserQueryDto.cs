using FluentValidation;

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
        public UserQueryDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .MaximumLength(50).WithMessage("NameFirst field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameFirst field can only contain letters, spaces, or hyphens!")
                .When(x => !string.IsNullOrEmpty(x.NameFirst));

            RuleFor(x => x.NameLast)
                .MaximumLength(50).WithMessage("NameLast field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameLast field can only contain letters, spaces, or hyphens!")
                .When(x => !string.IsNullOrEmpty(x.NameLast));

            RuleFor(x => x.Email)
                .MaximumLength(320).WithMessage("Email field cannot be larger than 320 characters!")
                .EmailAddress().WithMessage("Email field must be a valid email address!")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleForEach(x => x.UserRoles)
                .NotEmpty().WithMessage("Role item cannot be empty!")
                .MaximumLength(50).WithMessage("Role item name cannot exceed 50 characters!")
                .When(x => x.UserRoles != null);
        }
    }
}
