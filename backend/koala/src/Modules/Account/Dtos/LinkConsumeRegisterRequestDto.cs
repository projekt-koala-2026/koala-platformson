using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record LinkConsumeRegisterRequestDto
    (
        string NameFirst,
        string NameLast,
        string Password,
        bool AcceptedRodo,
        bool AcceptedRules
    );

    public class LinkConsumeRegisterRequestDtoValidator : AbstractValidator<LinkConsumeRegisterRequestDto>
    {
        public LinkConsumeRegisterRequestDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .NotEmpty().WithMessage("NameFirst field cannot be empty!")
                .MaximumLength(50).WithMessage("NameFirst field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.NameLast)
                .NotEmpty().WithMessage("NameLast field cannot be empty!")
                .MaximumLength(50).WithMessage("NameLast field cannot be larger than 50 characters!")
                .Matches(@"^[\p{L}\s-]+$").WithMessage("NameLast field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password field cannot be empty!")
                .MinimumLength(8).WithMessage("Password field must be at least 8 characters long!")
                .MaximumLength(50).WithMessage("Password field cannot be larger than 50 characters!")
                .Matches("[A-Z]").WithMessage("Password field must contain at least one uppercase letter!")
                .Matches("[a-z]").WithMessage("Password field must contain at least one lowercase letter!")
                .Matches("[0-9]").WithMessage("Password field must contain at least one number!")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password field must contain at least one special character!");

            RuleFor(x => x.AcceptedRodo)
                .Equal(true).WithMessage("AcceptedRodo field must be true!");

            RuleFor(x => x.AcceptedRules)
                .Equal(true).WithMessage("AcceptedRules field must be true!");
        }
    }
}