using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Services;
using koala.src.Shared.Core;
using Microsoft.CodeAnalysis.Differencing;
using koala.src.Shared.Account;

namespace koala.src.Modules.Core
{
    public static class CoreModuleExtensions
    {
        public static IServiceCollection AddCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            string dbConnStr = configuration.GetConnectionString("Local_Database_Postgres")!;
            string privateFilesPath = configuration["FileSettings:PrivateFilesPath"]!;

            // 1. DATABASES
            services.AddDbContext<CoreDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "core")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService<CoreSeederHostedService>();

            // 3. SERVICES
            services.AddScoped<ICoreModule, CoreService>();
            services.AddScoped<EditionService, EditionService>(sp =>
            {
                var db = sp.GetRequiredService<CoreDbContext>();
                var accountModule = sp.GetRequiredService<IAccountModule>();
                return new EditionService(db, accountModule, privateFilesPath);
            });
            services.AddScoped<SchoolService, SchoolService>();
            services.AddScoped<TaskService, TaskService>();
            services.AddScoped<RankingService, RankingService>();

            // 4. FLUID VALIDATORS

            return services;
        }
    }
}
