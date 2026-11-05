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

                .Must(HasNonEmptyStringProperty("sponsor_web_url"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'sponsor_web_url' field!")

                .Must(HasNonEmptyStringProperty("sponsor_logo_image_url"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'sponsor_logo_image_url' field!")

                .Must(HasNonEmptyStringProperty("body_pl"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'body_pl' field!")

                .Must(HasNonEmptyStringProperty("version"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'version' field!");
        }
    }
}
