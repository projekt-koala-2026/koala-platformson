using FluentValidation;

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
                .NotEmpty().WithMessage("Email field cannot be empty!")
                .MaximumLength(320).WithMessage("Email field cannot be larger than 50 characters!")
                .EmailAddress().WithMessage("Email field must be a valid email address!");
        }
    }
}