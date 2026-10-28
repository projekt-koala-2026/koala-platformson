using FluentValidation;

namespace koala.src.Modules.Account.Dtos
{
    public record EmailSendRequestDto
    (
        string MessgaeSubject,
        string MessageBody
    );

    public class EmailSendRequestDtoValidator : AbstractValidator<EmailSendRequestDto>
    {
        public EmailSendRequestDtoValidator()
        {
            RuleFor(x => x.MessgaeSubject)
                .NotEmpty().WithMessage("MessgaeSubject field cannot be empty!");

            RuleFor(x => x.MessageBody)
                .NotEmpty().WithMessage("MessageBody field cannot be empty!");
        }
    }
}