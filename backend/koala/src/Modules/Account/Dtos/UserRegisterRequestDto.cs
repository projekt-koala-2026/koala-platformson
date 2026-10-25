using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record UserRegisterRequestDto
    (
        string Email,
        List<string> Roles
    );

    public class UserRegisterRequestDtoValidator : AbstractValidator<UserRegisterRequestDto>
    {
        public UserRegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email field cannot be empty!")
                .MaximumLength(320).WithMessage("Email field cannot be larger than 50 characters!")
                .EmailAddress().WithMessage("Email field must be a valid email address!");
            RuleFor(x => x.Roles)
                .NotEmpty().WithMessage("Roles field cannot be empty!");
            RuleForEach(x => x.Roles)
                .NotEmpty().WithMessage("Role item cannot be empty!")
                .MaximumLength(50).WithMessage("Role item name cannot exceed 50 characters!");
        }
    }
}