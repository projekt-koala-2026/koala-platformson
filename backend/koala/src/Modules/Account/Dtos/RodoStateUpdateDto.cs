using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record RodoStateUpdateDto
    (
        string State,
        int Version
    );

    public class RodoStateUpdateDtoValidator : AbstractValidator<RodoStateUpdateDto>
    {
        public RodoStateUpdateDtoValidator()
        {
            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State field cannot be empty!")
                .MaximumLength(20).WithMessage("State field cannot exceed 20 characters!")
                .Must(state => state is "UNVERIFIED" or "VERIFIED")
                .WithMessage("State field must be either 'UNVERIFIED' or 'VERIFIED'!");
            RuleFor(x => x.Version)
                .GreaterThan(0).WithMessage("Version field must be greater than 0");
        }
    }
}