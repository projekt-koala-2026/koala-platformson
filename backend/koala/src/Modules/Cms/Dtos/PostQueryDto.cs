using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record PostQueryDto
    (
        Guid? EditionId,
        string? Name,
        bool? ShowHidden
    );

    public class PostQueryDtoDtoValidator : AbstractValidator<PostQueryDto>
    {
        public PostQueryDtoDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.Name))
                .WithErrorCode(KoalaValidationErrorCodes.PostNameTooLong)
                .WithMessage("Name field cannot be larger than 50 characters!");

            RuleFor(x => x.EditionId)
                .NotEqual(Guid.Empty)
                .When(x => x.EditionId.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.TeamEditionIdInvalid)
                .WithMessage("EditionId field must be a valid Guid7 ID!");
        }
    }
}
