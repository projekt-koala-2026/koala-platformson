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
        public DbSet<EditionSubEdition> EditionSubEditions => Set<EditionSubEdition>();
        public DbSet<SubEdition> SubEditions => Set<SubEdition>();
        public DbSet<SubEditionTask> SubEditionTasks => Set<SubEditionTask>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskItemTaskCorr> TaskItemTaskCorrs => Set<TaskItemTaskCorr>();
        public DbSet<TaskCorr> TaskCorrs => Set<TaskCorr>();
        public DbSet<TaskAnswer> TaskAnswers => Set<TaskAnswer>();
        public DbSet<School> Schools => Set<School>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<EditionSubEdition>().ToTable("edition_subeditions");
            modelBuilder.Entity<SubEdition>().ToTable("subeditions");
            modelBuilder.Entity<SubEditionTask>().ToTable("subedition_tasks");
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TaskItemTaskCorr>().ToTable("task_item_task_corrs");
            modelBuilder.Entity<TaskCorr>().ToTable("task_corrs");
            modelBuilder.Entity<TaskAnswer>().ToTable("task_answers");
            modelBuilder.Entity<School>().ToTable("schools");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e => e.Id);

            // SUBEDITIONS CONFIG
            modelBuilder.Entity<SubEdition>()
                .HasKey(s => s.Id);

            // EDITION-SUBEDITION CONFIG
            modelBuilder.Entity<EditionSubEdition>()
                .HasKey(es => new { es.EditionId, es.SubEditionId });
            modelBuilder.Entity<EditionSubEdition>()
                .HasOne(es => es.Edition)
                .WithMany(e => e.EditionSubEditions)
                .HasForeignKey(es => es.EditionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EditionSubEdition>()
                .HasOne(es => es.SubEdition)
                .WithOne(s => s.EditionSubEdition)
                .HasForeignKey<EditionSubEdition>(es => es.SubEditionId)
                .OnDelete(DeleteBehavior.Cascade);


            //TASKS CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.Id);

            // SUBEDITION-TASK CONFIG
            modelBuilder.Entity<SubEditionTask>()
                .HasKey(st => new { st.SubEditionId, st.TaskId });
            modelBuilder.Entity<SubEditionTask>()
                .HasOne(st => st.SubEdition)
                .WithMany(s => s.SubEditionTasks)
                .HasForeignKey(st => st.SubEditionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SubEditionTask>()
                .HasOne(st => st.TaskItem)
                .WithOne(t => t.SubEditionTask)
                .HasForeignKey<SubEditionTask>(st => st.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            //TASKCORRS CONFIG
            modelBuilder.Entity<TaskCorr>()
                .HasKey(tc => tc.Id);

            // TASKITEM-TASKCORR CONFIG
            modelBuilder.Entity<TaskItemTaskCorr>()
                .HasKey(ttc => new { ttc.TaskItemId, ttc.TaskCorrId });
            modelBuilder.Entity<TaskItemTaskCorr>()
                .HasOne(ttc => ttc.TaskItem)
                .WithMany(t => t.TaskItemTaskCorrs)
                .HasForeignKey(ttc => ttc.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaskItemTaskCorr>()
                .HasOne(ttc => ttc.TaskCorr)
                .WithOne(tc => tc.TaskItemTaskCorr)
                .HasForeignKey<TaskItemTaskCorr>(ttc => ttc.TaskCorrId)
                .OnDelete(DeleteBehavior.Cascade);

            //TASKANS CONFIG
            modelBuilder.Entity<TaskAnswer>()
                .HasKey(ta => new { ta.TeamId, ta.TaskId });
            modelBuilder.Entity<TaskAnswer>()
                .HasOne(ta => ta.TaskItem)
                .WithMany()
                .HasForeignKey(ta => ta.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskAnswer>()
                .Property(ta => ta.TeamId)
                    .IsRequired();
            modelBuilder.Entity<TaskAnswer>()
                .Property(ta => ta.TeamAnswer)
                    .IsRequired();

            modelBuilder.Entity<TaskAnswer>()
                .Property(ta => ta.Version)
                    .IsConcurrencyToken()
                    .IsRequired();

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s => s.Id);
        }
    };
}
