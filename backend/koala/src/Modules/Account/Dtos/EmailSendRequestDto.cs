using FluentValidation;
using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record EmailSendRequestDto
    (
        string MessageSubject,
        string MessageBody
    );

    public class EmailSendRequestDtoValidator : AbstractValidator<EmailSendRequestDto>
    {
        public EmailSendRequestDtoValidator()
        {
            RuleFor(x => x.MessageSubject)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.EmailSubjectEmpty)
                .WithMessage("MessageSubject field cannot be empty!");

            RuleFor(x => x.MessageBody)
                .NotEmpty()
                .WithErrorCode(KoalaValidationErrorCodes.EmailBodyEmpty)
                .WithMessage("MessageBody field cannot be empty!");
        }
    }
}