using koala.src.Modules.Cms.Entities;

using Microsoft.EntityFrameworkCore;


namespace koala.src.Modules.Cms.Data
{
    public class CmsDbContext : DbContext
    {
        public const string static_id_guid_page_home= "01a065c3-3bec-7224-b7b2-abc51fe1d87f";
        public const string static_id_guid_page_tasks = "01a065c3-3bec-7224-b7b2-afa2801aa4a4";
        public const string static_id_guid_page_history = "01a065c3-3bec-7224-b7b2-b241dc642361";
        public const string static_id_guid_page_rules = "01a065c3-3bec-7224-b7b2-b426015c004a";
        public const string static_id_guid_page_koalicjants = "01a065c3-3bec-7224-b7b2-b86c61973289";
        //public const string static_id_guid_page_sponsors = "01a065c3-3bec-7224-b7b2-bf60cb997f2e";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-c283ae044302";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-c6ac1f50dfc7";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-c978fc16348a";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-ce072bc36c30";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-d00f3251436e";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-d545ab96068e";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-d8ff8db41d82";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-ddbb09aa59e6";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-e0bf876fcfcf";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-e6346460839b";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-eb698cb17a59";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-efbd43f93231";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-f18b00fd0e6b";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-f4f35371c5d1";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-f9c2918f8385";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b2-ffa7aaa02d88";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-01fb471a66db";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-04a6afba8a1e";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-08eda07a40c0";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-0fe7ec2d8b28";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-12d501a0be93";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-15f454d70265";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-1992dfa8331b";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-1d14955efc30";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-2198cc1e5301";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-27dc7a27d304";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-29dd43c43966";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-2e61710155b3";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-32de567d5a5b";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-37fba25b50b5";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-39776abe53ae";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-3db30d6f0137";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-4067961fb889";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-470cb446cfdf";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-4bdaa46b8006";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-4ce00d167034";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-521b8a0bc84c";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-54c4a7e87bc8";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-598095b8b41a";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-5da0d425e7e2";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-6363e96a6c54";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-67fbbc24abd4";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-6a57ee3a8520";
        // public const string static_id_guid_page_? = "01a065c3-3bec-7224-b7b3-6cab833337fb";
        public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
        {

        }

        public DbSet<PublicFile> PublicFiles => Set<PublicFile>();
        public DbSet<StaticPage> StaticPages => Set<StaticPage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("cms");

            modelBuilder.Entity<PublicFile>().ToTable("public_files");
            modelBuilder.Entity<StaticPage>().ToTable("static_pages");

            // PUBLIC_FILE CONFIG
            modelBuilder.Entity<PublicFile>()
                .HasKey(pf=> pf.Id);

            // STATIC_PAGE CONFIG
            modelBuilder.Entity<StaticPage>()
                .HasKey(sp => sp.Id);
            
            Guid home_page_id = Guid.Parse(static_id_guid_page_home);
            Guid tasks_page_id = Guid.Parse(static_id_guid_page_tasks);
            Guid history_page_id = Guid.Parse(static_id_guid_page_history);
            Guid rules_page_id = Guid.Parse(static_id_guid_page_rules);
            Guid koalicjants_page_id = Guid.Parse(static_id_guid_page_koalicjants);
            modelBuilder.Entity<StaticPage>()
                .HasData
                (
                    new StaticPage{Id = home_page_id, Name = "HOME_PAGE", Path = $"/static-pages/{home_page_id}.json", UpdatedAt = null, Version = 1},
                    new StaticPage{Id = tasks_page_id, Name = "TASKS_PAGE", Path = $"/static-pages/{tasks_page_id}.json", UpdatedAt = null, Version = 1},
                    new StaticPage{Id = history_page_id, Name = "HISTORY_PAGE", Path = $"/static-pages/{history_page_id}.json", UpdatedAt = null, Version = 1},
                    new StaticPage{Id = rules_page_id, Name = "RULES_PAGE", Path = $"/static-pages/{rules_page_id}.json", UpdatedAt = null, Version = 1},
                    new StaticPage{Id = koalicjants_page_id, Name = "KOALICJANTS_PAGE", Path = $"/static-pages/{koalicjants_page_id}.json", UpdatedAt = null, Version = 1}
                );
        }
    };
}