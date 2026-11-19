using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamMemberQueryDto
    (
        Guid? MemberId
    );

    public class TeamMemberQueryDtoValidator : AbstractValidator<TeamMemberQueryDto>
    {
        public TeamMemberQueryDtoValidator()
        {
            RuleFor(x => x.MemberId)
                .NotEqual(Guid.Empty)
                .When(x => x.MemberId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamMemberIdInvalid)
                .WithMessage("MemberId field must be a valid Guid7 ID if provided!");
        }
    }
}