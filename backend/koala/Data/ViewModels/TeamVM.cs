using Microsoft.AspNetCore.Mvc;

namespace koala.Data.ViewModels
{
    public class TeamCreateVM
    {
        public string? TeamName { get; set; }
        public int? SchoolRSPO { get; set; }
        public string? Name1 { get; set; }
        public string? Name2 { get; set; }
        public string? Name3 { get; set; }
        public string? Name4 { get; set; }
    }

    public class TeamUpdateVM
    {

        public Guid Id { get; set; }
        public string? TeamName { get; set; }
        public int? SchoolRSPO { get; set; }
        public string? Name1 { get; set; }
        public string? Name2 { get; set; }
        public string? Name3 { get; set; }
        public string? Name4 { get; set; }
    }
}