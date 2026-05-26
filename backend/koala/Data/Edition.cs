using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Editions")]
    public class Edition
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public ICollection<Post> Posts { get; set; }
        public string History { get; set; }
    }

}