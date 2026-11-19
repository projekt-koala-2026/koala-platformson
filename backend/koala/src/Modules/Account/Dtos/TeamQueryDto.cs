using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamQueryDto
    (
        Guid? EditionId,
        Guid? SchoolId,
        Guid? CaptainId,
        Guid? GuardianId,
        string? Name
    );

    public class TeamQueryDtoValidator : AbstractValidator<TeamQueryDto>
    {
        public TeamQueryDtoValidator()
        {
            RuleFor(x => x.EditionId)
                .NotEqual(Guid.Empty)
                .When(x => x.EditionId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamEditionIdInvalid)
                .WithMessage("EditionId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.SchoolId)
                .NotEqual(Guid.Empty)
                .When(x => x.SchoolId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamSchoolIdInvalid)
                .WithMessage("SchoolId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.CaptainId)
                .NotEqual(Guid.Empty).When(x => x.CaptainId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamCaptainIdInvalid)
                .WithMessage("CaptainId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.GuardianId)
                .NotEqual(Guid.Empty)
                .When(x => x.GuardianId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamGuardianIdInvalid)
                .WithMessage("GuardianId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.Name)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.Name))
                .WithErrorCode(KoalaValidationErrorCodes.TeamNameTooLong)
                .WithMessage("Name field cannot be larger than 50 characters if provided!");
        }
    }
}