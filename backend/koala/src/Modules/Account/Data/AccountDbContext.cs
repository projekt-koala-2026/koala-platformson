using koala.src.Modules.Account.Entities;

using Microsoft.EntityFrameworkCore;


namespace koala.src.Modules.Account.Data
{
    public class AccountDbContext : DbContext
    {
        public const string static_id_guid_role_organization_admin = "01a027c5-d599-73de-bd5c-11f84a3fc125";
        public const string static_id_guid_role_organization_editor = "01a027c5-d599-73de-bd5c-14db087cc58f";
        public const string static_id_guid_role_organization_reviuer = "01a027c5-d599-73de-bd5c-18c05fc3fa53";
        public const string static_id_guid_role_team_admin = "01a027c5-d599-73de-bd5c-1f8e68687e51";
        public const string static_id_guid_role_team_player = "01a027c5-d599-73de-bd5c-2068e28b6cf3";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-2597ff8e14e5";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-285bbc57be72";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-2d755bf35dd9";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-31f9adea6147";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-34bf1ba2ebc5";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-3a141eabad41";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-3ea8fbcc485a";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-43dd6b88ce1e";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-450f6ae5714c";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-4b68aa4125e2";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-4d09ffe800b0";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-517bc7bdc306";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-547de2070140";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-58222770da53";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-5c75e9ce156a";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-601d86fb8a58";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-64f9d8c58852";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-6a8eaa590cc9";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-6de4b34ae252";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-7363acc9d2a3";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-7714b604160b";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-79602d82baf6";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-7c7c455e8814";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-834046d59493";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-86156617134e";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-880d29c24fb4";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-8e3f04d9275b";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-918be934cb4e";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-954eff71a6a1";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-9acbe86cef1c";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-9c8f0669fb33";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-a21232c551ec";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-a7f5bf38c705";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-a93993810bd0";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-ac4e6fc2ecb1";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-b25237cd7600";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-b475b8e6da7f";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-b830f0ac3397";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-bf4bbf5b6ba9";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-c28c2b1d82e4";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-c4277e2db4c6";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-c87c5a0bc764";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-cff5b05805c8";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-d2813f244abc";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-d71eb50e9625";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-da68a64f0428";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-dc05e793d390";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-e038a567114f";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-e4d66f23a7c4";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-ea00a4604d64";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-eef20b38b6b1";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-f00d88f885e7";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-f4d727888622";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-f91bedd9fb8e";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5c-fcd8677bd939";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-004d464d5f53";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-06edc22d6d3c";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-0bd8d8334a44";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-0e70fa35e647";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-1140bdc6215b";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-151eefbfbbcb";
        // private const string static_id_guid_ = "01a027c5-d599-73de-bd5d-1b5753754063";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-67e22b59850a";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-681a84b0f870";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-6eaefad369bf";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-70ec81db4d83";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-74b69100382f";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-78d40c12aa07";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-7df3f12c139b";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-81790611da24";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-854d6bcd50ce";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-8a479880db8e";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-8fa51bbb0679";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-904ed57f9bbc";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-96606eb1ae66";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-9b9874f143bd";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-9ec10dc636e4";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-a01cdb5233a5";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-a64abadbefec";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-a953e6b12628";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-ae4a3ed0d1b3";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-b3d58485a47b";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-b52bbca0e304";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-bbf8046be192";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-bf0e2124477a";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-c0087816bab8";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-c4ae7b8bc3b8";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-caa4158e15bd";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-cc1ad664985e";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-d3848badf84d";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-d42966f86893";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-d89087ac71e2";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-de8bc49a56ec";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-e39394489b78";
        // private const string static_id_guid_ = "01a027c5-d59a-724f-9c59-e58f6a5d63b9";
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Link> Links => Set<Link>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TeamJoinCode> TeamJoinCodes => Set<TeamJoinCode>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("account");

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<UserRole>().ToTable("user_roles");
            modelBuilder.Entity<Session>().ToTable("sessions");
            modelBuilder.Entity<Link>().ToTable("links");
            modelBuilder.Entity<Team>().ToTable("teams");
            modelBuilder.Entity<TeamMember>().ToTable("team_members");

            // ROLE CONFIG
            modelBuilder.Entity<Role>()
                .HasData
                (
                    new Role { Id = Guid.Parse(static_id_guid_role_organization_admin), Name = "ORGANIZATION_ADMIN"},
                    new Role { Id = Guid.Parse(static_id_guid_role_organization_editor), Name = "ORGANIZATION_EDITOR"},
                    new Role { Id = Guid.Parse(static_id_guid_role_organization_reviuer), Name = "ORGANIZATION_REVIUER"},
                    new Role { Id = Guid.Parse(static_id_guid_role_team_admin), Name = "TEAM_ADMIN"},
                    new Role { Id = Guid.Parse(static_id_guid_role_team_player), Name = "TEAM_PLAYER"}
                );


            // USER_ROLES CONFIG
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // TEAM_MEMBER CONFIG
            modelBuilder.Entity<TeamMember>()
                .HasKey(tm => new { tm.UserId, tm.TeamId });
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMembers)
                .HasForeignKey(tm => tm.UserId);

            // TEAM_JOIN_CODE CONFIG
            modelBuilder.Entity<TeamJoinCode>()
                .HasKey(tm => tm.TeamId);
            modelBuilder.Entity<TeamJoinCode>()
                .HasOne(tjc => tjc.Team)
                .WithOne(t => t.TeamJoinCode)
                .HasForeignKey<TeamJoinCode>(tjc => tjc.TeamId)
                .OnDelete(DeleteBehavior.Cascade);


            // SESSIONS CONFIG
            modelBuilder.Entity<Session>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // LINK CONFIG
            modelBuilder.Entity<Link>()
                .HasKey(l => l.Id);
            modelBuilder.Entity<Link>()
                .HasOne(l => l.User)
                .WithMany(u => u.Links)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    };
}