using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace koala.Data.ViewModels
{
    public class KoalicjantCreateVMValidator : AbstractValidator<KoalicjantCreateVM>
    {
        public KoalicjantCreateVMValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa koalicjanta jest wymagana.")
                .MaximumLength(150).WithMessage("Nazwa nie może przekraczać 150 znaków.");

            RuleFor(x => x.ProfilePicture)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL do zdjęcia profilowego.")
                .When(x => !string.IsNullOrEmpty(x.ProfilePicture));

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Opis nie może przekraczać 2000 znaków.")
                .When(x => x.Description != null);
        }

        private bool LinkMustBeAValidUri(string link)
        {
            return Uri.TryCreate(link, UriKind.RelativeOrAbsolute, out _);
        }
    }

    public class KoalicjantUpdateVMValidator : AbstractValidator<KoalicjantUpdateVM>
    {
        public KoalicjantUpdateVMValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Identyfikator (Id) jest wymagany do aktualizacji.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa koalicjanta jest wymagana.")
                .MaximumLength(150).WithMessage("Nazwa nie może przekraczać 150 znaków.");

            RuleFor(x => x.ProfilePicture)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL do zdjęcia profilowego.")
                .When(x => !string.IsNullOrEmpty(x.ProfilePicture));

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Opis nie może przekraczać 2000 znaków.")
                .When(x => x.Description != null);
        }

        private bool LinkMustBeAValidUri(string link)
        {
            return Uri.TryCreate(link, UriKind.RelativeOrAbsolute, out _);
        }
    }
}