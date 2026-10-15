 using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace koala.src.Modules.Cms.Data
{
    public class CmsDbContextFactory : IDesignTimeDbContextFactory<CmsDbContext>
    {
        public CmsDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var builder = new DbContextOptionsBuilder<CmsDbContext>();
            
            var connectionString = configuration.GetConnectionString("Local_Database_Postgres");

            builder.UseNpgsql(connectionString, npgsqlOptions => 
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "cms");
            });

            return new CmsDbContext(builder.Options);
        }
    }
}