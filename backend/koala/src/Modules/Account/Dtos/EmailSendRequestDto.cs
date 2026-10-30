using FluentValidation;

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
                .NotEmpty().WithMessage("MessageSubject field cannot be empty!");

            RuleFor(x => x.MessageBody)
                .NotEmpty().WithMessage("MessageBody field cannot be empty!");
        }
    }
}