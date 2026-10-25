using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamUpdateNameRequestDto
    (
        string Name
    );

    public class TeamUpdateNameRequestDtoValidator : AbstractValidator<TeamUpdateNameRequestDto>
    {
        public TeamUpdateNameRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name field cannot be empty!")
                .MaximumLength(50).WithMessage("Name field cannot be larger than 50 characters!");
        }
    }
}