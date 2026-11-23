using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record UpdatePublicFileRequestDto
    (
        string Name,
        string? Description
    );

    public class UpdatePublicFileRequestDtoValidator : AbstractValidator<UpdatePublicFileRequestDto>
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public UpdatePublicFileRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.PublicFileNameEmpty)
                .WithMessage("Name field cannot be empty!");

            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.PublicFileDescriptionEmpty)
                .WithMessage("Description field cannot be empty when not null!")
                .When(x => x.Description != null);
        }
    }
}
