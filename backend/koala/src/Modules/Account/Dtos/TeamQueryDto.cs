using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record TeamQueryDto
    (
        Guid? EditionId,
        Guid? SchoolId,
        Guid? CaptainId,
        Guid? AdminId,
        string? Name
    );

    public class TeamQueryDtoValidator : AbstractValidator<TeamQueryDto>
    {
        public TeamQueryDtoValidator()
        {
            RuleFor(x => x.EditionId)
                .NotEqual(Guid.Empty).When(x => x.EditionId.HasValue)
                .WithMessage("EditionId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.SchoolId)
                .NotEqual(Guid.Empty).When(x => x.SchoolId.HasValue)
                .WithMessage("SchoolId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.CaptainId)
                .NotEqual(Guid.Empty).When(x => x.CaptainId.HasValue)
                .WithMessage("CaptainId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.AdminId)
                .NotEqual(Guid.Empty).When(x => x.AdminId.HasValue)
                .WithMessage("AdminId field must be a valid Guid7 ID if provided!");

            RuleFor(x => x.Name)
                .MaximumLength(50).WithMessage("Name field cannot be larger than 50 characters if provided!")
                .When(x => !string.IsNullOrEmpty(x.Name));
        }
    }
}