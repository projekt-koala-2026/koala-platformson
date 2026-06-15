using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace koala.Data
{
    [Table("Teams")]
    public class Team
    {
        public Guid Id { get; set; }
        public Guid CaptainId { get; set; }
        public int? SchoolRSPO { get; set; }
        public string? TeamName { get; set; } = string.Empty;
        public string? Name1 { get; set; } = string.Empty;
        public string? Name2 { get; set; } = string.Empty;
        public string? Name3 { get; set; } = string.Empty;
        public string? Name4 { get; set; } = string.Empty;
    }
}