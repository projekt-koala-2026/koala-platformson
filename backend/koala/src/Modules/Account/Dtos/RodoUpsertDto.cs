using FluentValidation;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Account.Dtos
{
    public record RodoUpsertDto(
        IFormFile File,
        int? Version
    );

    public class RodoUpsertDtoValidator : AbstractValidator<RodoUpsertDto>
    {
        public RodoUpsertDtoValidator()
        {
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "application/pdf"
            };
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".pdf"
            };
            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.FileEmpty)
                .WithMessage("File cannot be empty!")

                .Must(file => file.Length > 0)
                .WithErrorCode(KoalaValidationErrorCodes.FileEmpty)
                .WithMessage("File cannot be empty!")

                .Must(file => file.Length <= 2 * 1024 * 1024)
                .WithErrorCode(KoalaValidationErrorCodes.FileToLarge)
                .WithMessage("File size cannot exceed 2 MB!")

                .Must(file => allowedContentTypes.Contains(file.ContentType.ToLower()))
                .WithErrorCode(KoalaValidationErrorCodes.FileInvalidType)
                .WithMessage("Only image files (JPEG, PNG, WebP) and PDF files are allowed!")

                .Must(file =>
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return allowedExtensions.Contains(extension);
                })
                .WithErrorCode(KoalaValidationErrorCodes.FileInvalidType)
                .WithMessage("Invalid file extension. Allowed extensions are: .jpg, .jpeg, .png, .webp, .pdf");

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .When(x => x.Version.HasValue)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version field must be greater than 0");
        }
    }
}
