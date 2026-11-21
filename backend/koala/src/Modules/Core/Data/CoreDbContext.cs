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
        public DbSet<Ranking> Rankings => Set<Ranking>();
        public DbSet<Winner> Winners => Set<Winner>();
        public DbSet<RankingData> RankingDatas => Set<RankingData>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions");
            modelBuilder.Entity<SubEdition>().ToTable("subeditions");
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TaskAnswer>().ToTable("task_answers");
            modelBuilder.Entity<TaskSubmission>().ToTable("task_submissions");
            modelBuilder.Entity<School>().ToTable("schools");
            modelBuilder.Entity<Ranking>().ToTable("rankings");

            modelBuilder.Entity<RankingData>().ToTable("ranking_datas");
            modelBuilder.Entity<Winner>().ToTable("winners");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Edition>()
                .HasOne(e => e.WinnerTier1)
                .WithOne()
                .HasForeignKey<Edition>(e => e.WinnerTier1Id)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Edition>()
                .HasOne(e => e.WinnerTier2)
                .WithOne()
                .HasForeignKey<Edition>(e => e.WinnerTier2Id)
                .OnDelete(DeleteBehavior.SetNull);

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
                .Property(ts => ts.Version)
                .IsConcurrencyToken();

            modelBuilder.Entity<TaskSubmission>()
                .HasOne(ts => ts.TaskItem)
                .WithMany(t => t.TaskSubmissions)
                .HasForeignKey(ts => ts.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // SCHOOLS CONFIG
            modelBuilder.Entity<School>()
                .HasKey(s => s.Id);

            // RANKING CONFIG
            modelBuilder.Entity<Ranking>()
                .HasKey(r => r.RankingId);
            modelBuilder.Entity<Ranking>()
                .Property(rd => rd.Version)
                .IsConcurrencyToken();
            modelBuilder.Entity<Ranking>()
                .HasOne(r => r.Edition)
                .WithMany(e => e.Rankings)
                .HasForeignKey(r => r.EditionId)
                .OnDelete(DeleteBehavior.Cascade);

            // WINNER CONFIG
            modelBuilder.Entity<Winner>()
                .HasKey(w => w.Id);
            modelBuilder.Entity<Winner>()
                .Property(w => w.Version)
                .IsConcurrencyToken();

            // RANKING DATA CONFIG
            modelBuilder.Entity<RankingData>()
                .HasKey(rd => rd.Id);
            modelBuilder.Entity<RankingData>()
                .Property(rd => rd.Version)
                .IsConcurrencyToken();
            modelBuilder.Entity<RankingData>()
                .HasOne(rd => rd.Ranking)
                .WithMany(r => r.RankingDatas)
                .HasForeignKey(rd => rd.RankingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    };
}
