using FluentValidation;
using koala.src.Shared;
using System.Text.Json;

namespace koala.src.Modules.Cms.Dtos
{
    public record UpdateStaticPageRequestDto
    (
        string ContentJson,
        int Version
    );

    public class UpdateStaticPageRequestDtoValidator : AbstractValidator<UpdateStaticPageRequestDto>
    {
        private static bool IsValidJson(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }
        
            try
            {
                using var doc = JsonDocument.Parse(content);
                return doc.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch
            {
                return false;
            }
        }
        
        private static Func<string?, bool> HasNonEmptyStringProperty(string propertyName)
        {
            return content =>
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return false;
                }
        
                try
                {
                    using var doc = JsonDocument.Parse(content);
        
                    return doc.RootElement.TryGetProperty(propertyName, out var property)
                        && property.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(property.GetString());
                }
                catch
                {
                    return false;
                }
            };
        }
        public UpdateStaticPageRequestDtoValidator()
        {
            RuleFor(x => x.ContentJson)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonEmpty)
                .WithMessage("ContentJson field cannot be empty!")

                .Must(IsValidJson)
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonInvalid)
                .WithMessage("ContentJson field must be a valid json!")

                .Must(HasNonEmptyStringProperty("body_pl"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'body_pl' field!")

                .Must(HasNonEmptyStringProperty("version"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'version' field!");

            RuleFor(x => x.Version)
                .GreaterThan(0)
                .WithErrorCode(KoalaValidationErrorCodes.ResourceVersionInvalid)
                .WithMessage("Version cannot be 0 or less");
        }
    }
}