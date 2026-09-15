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
        public DbSet<Subedition> SubEditions => Set<Subedition>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.HasDefaultSchema("core");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>(b =>
            {
                b.ToTable("editions");
                b.HasKey(e => e.Id);
            });

            // SUBEDITIONS CONFIG
            modelBuilder.Entity<Subedition>(b =>
            {
                b.ToTable("subeditions");
                b.HasKey(s => s.Id);

                b.HasOne(s => s.Edition)
                    .WithMany(e => e.SubEditions)
                    .HasForeignKey(s => s.EditionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    };
}