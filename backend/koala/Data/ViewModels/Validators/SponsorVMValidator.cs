using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace koala.Data.ViewModels
{
    public class SponsorCreateVMValidator : AbstractValidator<SponsorCreateVM>
    {
        public SponsorCreateVMValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa sponsora jest wymagana.")
                .MaximumLength(100).WithMessage("Nazwa nie może przekraczać 100 znaków.");

            RuleFor(x => x.WebsiteUrl)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL do strony sponsora.")
                .When(x => !string.IsNullOrEmpty(x.WebsiteUrl));

            RuleFor(x => x.LogoUrl)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL do logo.")
                .When(x => !string.IsNullOrEmpty(x.LogoUrl));
                
        }

        private bool LinkMustBeAValidUri(string link)
        {
            return Uri.TryCreate(link, UriKind.Absolute, out var outUri)
                   && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class SponsorUpdateVMValidator : AbstractValidator<SponsorUpdateVM>
    {
        public SponsorUpdateVMValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nazwa sponsora jest wymagana.")
                .MaximumLength(100).WithMessage("Nazwa nie może przekraczać 100 znaków.");

            RuleFor(x => x.WebsiteUrl)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL.")
                .When(x => !string.IsNullOrEmpty(x.WebsiteUrl));

            RuleFor(x => x.LogoUrl)
                .Must(LinkMustBeAValidUri).WithMessage("Podaj poprawny adres URL.")
                .When(x => !string.IsNullOrEmpty(x.LogoUrl));
        }

        private bool LinkMustBeAValidUri(string link)
        {
            return Uri.TryCreate(link, UriKind.Absolute, out var outUri)
                   && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
        }
    }
}