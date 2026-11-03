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
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<TaskAnswer> TaskAnswers => Set<TaskAnswer>();
        public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
        public DbSet<School> Schools => Set<School>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<SubEdition>().ToTable("subeditions");
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TaskAnswer>().ToTable("task_answers");
            modelBuilder.Entity<TaskSubmission>().ToTable("task_submissions");
            modelBuilder.Entity<School>().ToTable("schools");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e => e.Id);

            // SUBEDITIONS CONFIG
            modelBuilder.Entity<SubEdition>()
                .HasKey(s => s.Id);

            // EDITION-SUBEDITION CONFIG
            modelBuilder.Entity<SubEdition>()
                .HasOne(s => s.Edition)
                .WithMany(e => e.SubEditions)
                .HasForeignKey(s => s.EditionId)
                .OnDelete(DeleteBehavior.Cascade);


            //TASKS CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.Id);

            // SUBEDITION-TASK CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.SubEdition)
                .WithMany(s => s.TaskItems)
                .HasForeignKey(t => t.SubEditionId)
                .OnDelete(DeleteBehavior.Cascade);

            //TASKANSWERS CONFIG
            modelBuilder.Entity<TaskAnswer>()
                .HasKey(ta => ta.Id);

            // TASKITEM-TASKANSWER CONFIG
            modelBuilder.Entity<TaskAnswer>()
                .HasOne(t => t.TaskItem)
                .WithMany(ta => ta.TaskAnswers)
                .HasForeignKey(t => t.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            //TASKSUBMISSIONS CONFIG
            modelBuilder.Entity<TaskSubmission>()
                .HasKey(ts => new { ts.TeamId, ts.TaskId });

            modelBuilder.Entity<TaskSubmission>()
                .HasOne(ts => ts.TaskItem)
                .WithMany(t => t.TaskSubmissions)
                .HasForeignKey(ts => ts.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s => s.Id);
        }
    };
}
