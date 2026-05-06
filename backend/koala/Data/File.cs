using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("PublicFiles")]
    public class PublicFile
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Folder { get; set; }
        public string FilePath { get; set; }
    }

}