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
        public DbSet<Etap1Series> Etap1Series => Set<Etap1Series>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<TaskAnswer> TaskAnswers => Set<TaskAnswer>();
        public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
        public DbSet<School> Schools => Set<School>();
        public DbSet<Ranking> Rankings => Set<Ranking>();
        public DbSet<RankingData> RankingDatas => Set<RankingData>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("core");

            modelBuilder.Entity<Edition>().ToTable("editions", table =>
                table.HasCheckConstraint("CK_editions_distinct_total_rankings", "tier1_ranking_total_id <> tier2_ranking_total_id"));
            modelBuilder.Entity<Etap1Series>().ToTable("etap1series", table =>
                table.HasCheckConstraint("CK_etap1series_distinct_rankings", "tier1_ranking_id <> tier2_ranking_id"));
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TaskAnswer>().ToTable("task_answers");
            modelBuilder.Entity<TaskSubmission>().ToTable("task_submissions");
            modelBuilder.Entity<School>().ToTable("schools");
            modelBuilder.Entity<Ranking>().ToTable("rankings");

            modelBuilder.Entity<RankingData>().ToTable("ranking_datas");

            // EDITIONS CONFIG
            modelBuilder.Entity<Edition>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Edition>()
                .HasOne(e => e.Tier1RankingTotal)
                .WithOne()
                .HasForeignKey<Edition>(e => e.Tier1RankingTotalId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Edition>()
                .HasOne(e => e.Tier2RankingTotal)
                .WithOne()
                .HasForeignKey<Edition>(e => e.Tier2RankingTotalId)
                .OnDelete(DeleteBehavior.Restrict);
                

            // Etap1Series CONFIG
            modelBuilder.Entity<Etap1Series>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Etap1Series>()
                .HasOne(s => s.Edition)
                .WithMany(e => e.Etap1Series)
                .HasForeignKey(s => s.EditionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Etap1Series>()
                .HasOne(e => e.Tier1Ranking)
                .WithOne()
                .HasForeignKey<Etap1Series>(e => e.Tier1RankingId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Etap1Series>()
                .HasOne(e => e.Tier2Ranking)
                .WithOne()
                .HasForeignKey<Etap1Series>(e => e.Tier2RankingId)
                .OnDelete(DeleteBehavior.Restrict);

            //TASKS CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasKey(t => t.Id);

            // Etap1Series-TASK CONFIG
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Etap1Series)
                .WithMany(s => s.TaskItems)
                .HasForeignKey(t => t.Etap1SeriesId)
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
