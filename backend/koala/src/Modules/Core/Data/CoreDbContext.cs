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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            
            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e=> e.Id);
        }
    };
}