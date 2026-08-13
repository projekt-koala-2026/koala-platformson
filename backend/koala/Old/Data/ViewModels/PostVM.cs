using System.ComponentModel.DataAnnotations;

namespace koala.Data.ViewModels
{
    public class PostCreateVM
    {
        [Required(ErrorMessage = "Tytuł postu jest wymagany.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Tytuł musi mieć od 3 do 200 znaków.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Treść (Markdown) nie może być pusta.")]
        public string MarkdownBody { get; set; }

        [Required(ErrorMessage = "Post musi być przypisany do edycji.")]
        public Guid EditionId { get; set; }
    }
    public class PostUpdateVM
    {
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string MarkdownBody { get; set; }

        [Required]
        public Guid EditionId { get; set; }
    }
}
