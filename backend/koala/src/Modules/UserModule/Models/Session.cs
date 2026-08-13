namespace koala.src.Modules.UserModule.Models;

public class Session
{
    public Guid UserId { get; set; } ;
    public Guid Token { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime RevokeAt { get; set; } = DateTime.UtcNow;
}