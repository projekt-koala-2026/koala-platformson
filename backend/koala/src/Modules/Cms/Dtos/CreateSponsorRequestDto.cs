using System.Text.Json;
using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record CreateSponsorRequestDto
    (
        string Name,
        string ContentJson,
        bool IsVisible
    );

    public class CreateSponsorRequestDtoValidator : AbstractValidator<CreateSponsorRequestDto>
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
        private static Func<string?, bool> HasStringOrNullProperty(string propertyName)
        {
            return content =>
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                try
                {
                    using var doc = JsonDocument.Parse(content);

                    return doc.RootElement.TryGetProperty(propertyName, out var property)
                        && (property.ValueKind == JsonValueKind.String
                            || property.ValueKind == JsonValueKind.Null);
                }
                catch
                {
                    return false;
                }
            };
        }
        public CreateSponsorRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.SponsorNameEmpty)
                .WithMessage("NameFirst field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.SponsorNameTooLong)
                .WithMessage("NameFirst field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.SponsorNameInvalidCharacter)
                .WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.ContentJson)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonEmpty)
                .WithMessage("ContentJson field cannot be empty!")

                .Must(IsValidJson)
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonInvalid)
                .WithMessage("ContentJson field must be a valid json!")

                .Must(HasStringOrNullProperty("sponsor_web_url"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid string for 'sponsor_web_url' or null!")

                .Must(HasStringOrNullProperty("sponsor_logo_image_url"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid string for 'sponsor_logo_image_url' or null!")

                .Must(HasStringOrNullProperty("sponsor_type"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid string for 'sponsor_type' or null!")

                .Must(HasNonEmptyStringProperty("body_pl"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'body_pl' field!")

                .Must(HasNonEmptyStringProperty("version"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'version' field!");
        }
    }
}
