using System.ComponentModel.DataAnnotations;

namespace koala.Data.ViewModels
{
    public class TeamCreateVM
    {
        [Required]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        public string Name1 { get; set; } = string.Empty;

        [Required]
        public string Name2 { get; set; } = string.Empty;

        [Required]
        public string Name3 { get; set; } = string.Empty;

        [Required]
        public string Name4 { get; set; } = string.Empty;

    }

    public class TeamUpdateVM
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        public string Name1 { get; set; } = string.Empty;

        [Required]
        public string Name2 { get; set; } = string.Empty;

        [Required]
        public string Name3 { get; set; } = string.Empty;

        [Required]
        public string Name4 { get; set; } = string.Empty;

    }
}