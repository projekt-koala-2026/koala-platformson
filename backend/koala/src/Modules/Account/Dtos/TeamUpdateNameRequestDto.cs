using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamUpdateNameRequestDto
    (
        string Name,
        int Version
    );

    public class TeamUpdateNameRequestDtoValidator : AbstractValidator<TeamUpdateNameRequestDto>
    {
        public TeamUpdateNameRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.TeamNameEmpty)
                .WithMessage("Name field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.TeamNameTooLong)
                .WithMessage("Name field cannot be larger than 50 characters!");

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}