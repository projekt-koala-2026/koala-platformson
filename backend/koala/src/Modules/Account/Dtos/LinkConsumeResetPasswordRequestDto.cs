using FluentValidation;

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
                .NotEmpty().WithMessage("Password field cannot be empty!")
                .MinimumLength(8).WithMessage("Password field must be at least 8 characters long!")
                .MaximumLength(50).WithMessage("Password field cannot be larger than 50 characters!")
                .Matches("[A-Z]").WithMessage("Password field must contain at least one uppercase letter!")
                .Matches("[a-z]").WithMessage("Password field must contain at least one lowercase letter!")
                .Matches("[0-9]").WithMessage("Password field must contain at least one number!")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password field must contain at least one special character!");
        }
    }
}