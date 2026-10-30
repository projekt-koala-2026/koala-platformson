using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Account.Dtos
{
    public record RodoUpsertDto(
        IFormFile File
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
                .NotNull().WithMessage("File cannot be null!")
                .Must(file => file.Length > 0).WithMessage("File cannot be empty!")
                .Must(file => file.Length <= 2 * 1024 * 1024).WithMessage("File size cannot exceed 2 MB!")
                .Must(file => allowedContentTypes.Contains(file.ContentType.ToLower()))
                .WithMessage("Only image files (JPEG, PNG, WebP) and PDF files are allowed!")
                .Must(file =>
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return allowedExtensions.Contains(extension);
                }).WithMessage("Invalid file extension. Allowed extensions are: .jpg, .jpeg, .png, .webp, .pdf");
        }
    }
}