 using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace koala.src.Modules.Account.Data
{
    public class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
    {
        public AccountDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var builder = new DbContextOptionsBuilder<AccountDbContext>();
            
            var connectionString = configuration.GetConnectionString("Local_Database_Postgres");

            builder.UseNpgsql(connectionString, npgsqlOptions => 
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "account");
            });

            return new AccountDbContext(builder.Options);
        }
    }
}