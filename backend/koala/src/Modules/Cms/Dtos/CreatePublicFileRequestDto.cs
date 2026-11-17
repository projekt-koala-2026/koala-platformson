using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record CreatePublicFileRequestDto
    (
        string Name,
        IFormFile File
    );

    public class CreatePublicFileRequestDtoValidator : AbstractValidator<CreatePublicFileRequestDto>
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public CreatePublicFileRequestDtoValidator()
        {
            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.FileEmpty)
                .WithMessage("File cannot be empty!")

                .Must(file => file.Length > 0)
                .WithErrorCode(KoalaValidationErrorCodes.FileEmpty)
                .WithMessage("File cannot be empty!")

                .Must(file => file.Length <= MaxFileSizeBytes)
                .WithErrorCode(KoalaValidationErrorCodes.FileToLarge)
                .WithMessage("File size cannot exceed 5 MB!")

                .Must(file => PublicFileTypes.ALLOWED_CONTENT_TYPES.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                .WithErrorCode(KoalaValidationErrorCodes.FileInvalidType)
                .WithMessage("File content type is not supported.")

                .Must(file => PublicFileTypes.ALLOWED_EXTENSIONS.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
                .WithErrorCode(KoalaValidationErrorCodes.FileInvalidType)
                .WithMessage($"Invalid file extension. Allowed extensions are: {string.Join(", ", PublicFileTypes.ALLOWED_EXTENSIONS)}");
        }
    }
}
