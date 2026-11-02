using System.ComponentModel.DataAnnotations.Schema;

namespace koala.src.Modules.Account.Entities
{
    public static class RoleNames
    {
        public static string NAME_ORGANIZATION_ADMIN = "ORGANIZATION_ADMIN";
        public static string NAME_ORGANIZATION_EDITOR = "ORGANIZATION_EDITOR";
        public static string NAME_ORGANIZATION_REVIEWER = "ORGANIZATION_REVIEWER";
        public static string NAME_TEAM_CAPTAIN = "TEAM_CAPTAIN";
        public static string NAME_TEAM_GUARDIAN = "TEAM_GUARDIAN";
    }
    public class Role
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}