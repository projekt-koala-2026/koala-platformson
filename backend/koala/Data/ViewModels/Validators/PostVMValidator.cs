using FluentValidation;

namespace koala.Data.ViewModels
{
    public class PostCreateVMValidator : AbstractValidator<PostCreateVM>
    {
        public PostCreateVMValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tytuł postu jest wymagany.")
                .Length(3, 200).WithMessage("Tytuł musi mieć od 3 do 200 znaków.");

            RuleFor(x => x.MarkdownBody)
                .NotEmpty().WithMessage("Treść (Markdown) nie może być pusta.");

            RuleFor(x => x.EditionId)
                .NotEmpty().WithMessage("Post musi być przypisany do edycji.");
        }
    }

    public class PostUpdateVMValidator : AbstractValidator<PostUpdateVM>
    {
        public PostUpdateVMValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.MarkdownBody)
                .NotEmpty();

            RuleFor(x => x.EditionId)
                .NotEmpty();
        }
    }
}