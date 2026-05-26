using System.ComponentModel.DataAnnotations;

namespace koala.Data.ViewModels
{
    public class SponsorCreateVM
    {
        [Required(ErrorMessage = "Nazwa sponsora jest wymagana.")]
        [StringLength(100, ErrorMessage = "Nazwa nie może przekraczać 100 znaków.")]
        public string Name { get; set; }

        [Url(ErrorMessage = "Podaj poprawny adres URL do strony sponsora.")]
        public string WebsiteUrl { get; set; }

        [Url(ErrorMessage = "Podaj poprawny adres URL do logo.")]
        public string LogoUrl { get; set; }

        public string Description { get; set; }
    }

    public class SponsorUpdateVM
    {
        [Required(ErrorMessage = "Nazwa sponsora jest wymagana.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Url]
        public string WebsiteUrl { get; set; }

        [Url]
        public string LogoUrl { get; set; }

        public string Description { get; set; }
    }
}
