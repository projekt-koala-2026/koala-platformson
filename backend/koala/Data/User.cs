using System.ComponentModel.DataAnnotations;

namespace koala.Data
{
    public enum UserRoles
    {
        NONE = 0x0000,
        ADMIN = 0x1111,
        EDITOR = 0x1000,
        REVIEWER = 0x0100,
        CAPTAIN = 0x0010,
        GUARDIAN = 0x0001
    }

    public class User
    {
        [Key]
        public Guid id = Guid.NewGuid();
        public string email = "";
        public string password= "";
        public UserRoles role = UserRoles.NONE;
        public Guid sessionToken = Guid.Empty;
        public DateTimeOffset sessionTokenCreationTime = DateTimeOffset.MinValue;
    }
}
