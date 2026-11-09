using FluentValidation;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;

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
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.RodoStateEmpty)
                .WithMessage("State field cannot be empty!")

                .MaximumLength(20)
                .WithErrorCode(KoalaValidationErrorCodes.RodoStateTooLong)
                .WithMessage("State field cannot exceed 20 characters!")

                .Must(state => state is TeamMemberRodoState.RODOSTATE_ACCEPETED or TeamMemberRodoState.RODOSTATE_REJECTED)
                .WithErrorCode(KoalaValidationErrorCodes.RodoStateInvalid)
                .WithMessage($"State field must be either {TeamMemberRodoState.RODOSTATE_ACCEPETED} or {TeamMemberRodoState.RODOSTATE_REJECTED}!");

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}