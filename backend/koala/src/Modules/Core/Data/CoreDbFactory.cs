using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace koala.src.Modules.Core.Data
{
    public class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
    {
        public CoreDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var builder = new DbContextOptionsBuilder<CoreDbContext>();
            
            var connectionString = configuration.GetConnectionString("Local_Database_Postgres");

            builder.UseNpgsql(connectionString, npgsqlOptions => 
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "core");
            });

            return new CoreDbContext(builder.Options);
        }
    }
}