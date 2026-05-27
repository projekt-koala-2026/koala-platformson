using Microsoft.EntityFrameworkCore;

namespace koala.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<PublicFile> PublicFiles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Edition> Editions { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
        public DbSet<Koalicjant> Koalicjants { get; set; }
        public DbSet<School> Schools { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Value)
                .IsUnique();

            modelBuilder.Entity<Token>()
                .HasOne(t => t.User)
                .WithOne(u => u.token)
                .HasForeignKey<Token>(t => t.UserId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}

