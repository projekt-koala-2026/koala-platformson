using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Schools")]
    public class School
    {
        [Key]
        public int RSPO { get; set; }
        public string Name { get; set; }
        public string? NameShort { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Type { get; set; }
        public string Addres { get; set; }
    }
}
