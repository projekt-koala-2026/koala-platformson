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
<<<<<<< HEAD
        public DbSet<Subedition> SubEditions => Set<Subedition>();
=======
        public DbSet<School> Schools => Set<School>();
>>>>>>> origin/backend_refactor
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.HasDefaultSchema("core");

<<<<<<< HEAD
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
=======
            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<School>().ToTable("schools");
            
            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e=> e.Id);

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s=> s.Id);
>>>>>>> origin/backend_refactor
        }
    };
}