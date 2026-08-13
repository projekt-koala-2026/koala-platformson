using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Sponsors")]
    public class Sponsor
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string WebsiteUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
