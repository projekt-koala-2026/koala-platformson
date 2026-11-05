using System.Text.Json;
using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Cms.Dtos
{
    public record CreatePostRequestDto
    (
        Guid EditionId,
        string Name,
        string ContentJson,
        bool IsVisible
    );

    public class CreatePostRequestDtoValidator : AbstractValidator<CreatePostRequestDto>
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
        public CreatePostRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.PostNameEmpty)
                .WithMessage("NameFirst field cannot be empty!")

                .MaximumLength(50)
                .WithErrorCode(KoalaValidationErrorCodes.PostNameTooLong)
                .WithMessage("NameFirst field cannot be larger than 50 characters!")

                .Matches(@"^[\p{L}\s-]+$")
                .WithErrorCode(KoalaValidationErrorCodes.PostNameInvalidCharacter)
                .WithMessage("NameFirst field can only contain letters, spaces, or hyphens!");

            RuleFor(x => x.EditionId)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.TeamEditionIdEmpty)
                .WithMessage("EditionId field cannot be empty!")

                .NotEqual(Guid.Empty)
                .WithErrorCode(KoalaValidationErrorCodes.TeamEditionIdInvalid)
                .WithMessage("EditionId field must be a valid Guid7 ID!");

            RuleFor(x => x.ContentJson)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonEmpty)
                .WithMessage("ContentJson field cannot be empty!")

                .Must(IsValidJson)
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonInvalid)
                .WithMessage("ContentJson field must be a valid json!")

                .Must(HasNonEmptyStringProperty("title_pl"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'title_pl' field!")

                .Must(HasNonEmptyStringProperty("body_pl"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'body_pl' field!")

                .Must(HasNonEmptyStringProperty("version"))
                .WithErrorCode(KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)
                .WithMessage("ContentJson filed must contain valid 'version' field!");
        }
    }
}
