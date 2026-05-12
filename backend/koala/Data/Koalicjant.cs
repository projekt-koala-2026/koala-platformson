using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Koalicjanci")]
    public class Koalicjant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string ProfilePicture {get; set; }

        public string Description { get; set; }
    }
}