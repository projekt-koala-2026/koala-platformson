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
        public DbSet<SubEdition> SubEditions => Set<SubEdition>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<School> Schools => Set<School>();
        public DbSet<TaskOdp> TaskOdps => Set<TaskOdp>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<SubEdition>().ToTable("subeditions");
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TaskOdp>().ToTable("task_odps");
            modelBuilder.Entity<School>().ToTable("schools");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e => e.Id);
            
            // SUBEDITIONS CONFIG
            modelBuilder.Entity<SubEdition>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<SubEdition>()
                .HasOne(s => s.Edition)
                .WithMany(e => e.SubEditions)
                .HasForeignKey(s => s.EditionId)
                .OnDelete(DeleteBehavior.Cascade);

            //TASKS CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.Id);

            //TASKODPS CONFIG
            modelBuilder.Entity<TaskOdp>()
                .HasKey(t => t.Id);

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s => s.Id);
        }
    };
}
