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
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ICollection<Post> Posts { get; set; }
    }

}