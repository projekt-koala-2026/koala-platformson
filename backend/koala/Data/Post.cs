using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Body { get; set; }
        public Guid PublicFileId { get; set; }
        public DateTime CreatedAt { get; set; }

        public PublicFile PublicFiles {get; set;}
    }

}