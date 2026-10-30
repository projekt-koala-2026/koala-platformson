using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamCreateRequestDto
    (
        string Name,
        Guid SchoolId
    );

    public class TeamCreateRequestDtoValidator : AbstractValidator<TeamCreateRequestDto>
    {
        public TeamCreateRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name field cannot be empty!")
                .MaximumLength(50).WithMessage("Name field cannot be larger than 50 characters!");
            RuleFor(x => x.SchoolId)
                .NotEmpty().WithMessage("SchoolId field cannot be empty!")
                .NotEqual(Guid.Empty).WithMessage("SchoolId field must be a valid Guid7 ID!");
        }
    }
}