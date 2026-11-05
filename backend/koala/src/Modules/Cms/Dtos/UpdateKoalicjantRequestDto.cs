using System.Text.Json;
using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record UpdateKoalicjantRequestDto
    (
        string NameFirst,
        string NameLast,
        string Email,
        string ContentJson,
        bool IsVisible,
        int Version
    );

    public class UpdateKoalicjantRequestDtoValidator : AbstractValidator<UpdateKoalicjantRequestDto>
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
        public UpdateKoalicjantRequestDtoValidator()
        {
            RuleFor(x => x.NameFirst)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstEmpty)
                .WithMessage("NameFirst field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstTooLong)
                .WithMessage("NameFirst field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.NameFirstInvalidCharacter)
                .WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.NameLast)

                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.NameLastEmpty)
                .WithMessage("NameLast field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.NameLastTooLong)
                .WithMessage("NameLast field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.NameLastInvalidCharacter)
                .WithMessage("NameLast field can only contain letters, spaces, or hyphens!");
            
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.EmailEmpty)
                .WithMessage("Email field cannot be empty!")

                .MaximumLength(320)
                .WithErrorCode(KoalaValidationErrorCodes.EmailTooLong)
                .WithMessage("Email field cannot be larger than 320 characters!")

                .EmailAddress()
                .WithErrorCode(KoalaValidationErrorCodes.EmailInvalid)
                .WithMessage("Email field must be a valid email address!");

            RuleFor(x => x.ContentJson)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonEmpty)
                .WithMessage("ContentJson field cannot be empty!")

                .Must(IsValidJson)
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonInvalid)
                .WithMessage("ContentJson field must be a valid json!")

                .Must(HasNonEmptyStringProperty("koalicjant_profile_image_url"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'profile_image_url' field!")

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
