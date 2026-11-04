using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record UserCensorRequestDto
    (
        int Version
    );

    public class UserCensorRequestDtoValidator : AbstractValidator<UserCensorRequestDto>
    {
        public UserCensorRequestDtoValidator()
        {
            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}