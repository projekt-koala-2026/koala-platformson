using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record LoginRequestDto
    (
        string Email,
        string Password
    );

    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email field cannot be empty!")
                .MaximumLength(320).WithMessage("Email field cannot be larger than 50 characters!");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password field cannot be empty!")
                .MaximumLength(50).WithMessage("Password field cannot be larger than 50 characters!");
        }
    }
}