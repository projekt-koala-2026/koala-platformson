namespace koala.Data.ViewModels
{
    public class UserVM
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public List<string>? Roles { get; set; }
    }
}