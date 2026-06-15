using System.ComponentModel.DataAnnotations;

namespace koala.Data.ViewModels
{
    public class PostCreateVM
    {
        public string Title { get; set; }
        public string MarkdownBody { get; set; }
        public Guid EditionId { get; set; }
    }

    public class PostUpdateVM
    {
        public string Title { get; set; }
        public string MarkdownBody { get; set; }
        public Guid EditionId { get; set; }
    }
}
