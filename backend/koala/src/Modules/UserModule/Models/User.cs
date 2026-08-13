namespace koala.src.Modules.UserModule.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public String NameFirst { get; set; } = String.Empty;
    public String NameLast { get; set; } = String.Empty;
    public String Email { get; set; } = String.Empty;
    public String PasswordHash { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Bool IsVerified { get; set; }
}