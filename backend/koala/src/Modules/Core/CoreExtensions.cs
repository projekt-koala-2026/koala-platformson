using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Services;
using koala.src.Shared.Core;

namespace koala.src.Modules.Core
{
    public static class CoreModuleExtensions
    {
        public static IServiceCollection AddCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            string dbConnStr = configuration.GetConnectionString("Local_Database_Postgres")!;

            // 1. DATABASES
            services.AddDbContext<CoreDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions =>npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "core")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)

            // 3. SERVICES
            services.AddScoped<ICoreModule, CoreService>();
            services.AddScoped<EditionService, EditionService>();
        
            // 4. EXCEPTION HANDLERS
            services.AddExceptionHandler<CoreExceptionHandler>();
            services.AddProblemDetails();

            // 5. FLUID VALIDATORS

            return services;
        }
    }
}