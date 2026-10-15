using koala.src.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace koala.src.Modules.Core.Data
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
        {

        }

        public DbSet<Edition> Editions => Set<Edition>();
        public DbSet<School> Schools => Set<School>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<School>().ToTable("schools");
            
            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e=> e.Id);

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s=> s.Id);
        }
    };
}