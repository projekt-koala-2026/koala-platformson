using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Services;
using Microsoft.Extensions.FileProviders;


namespace koala.src.Modules.Cms
{
    public static class CmsModuleExtensions
    {
        private static string publicFilesPath = "/app/public_files";
        public static IServiceCollection AddCmsModule(this IServiceCollection services, IConfiguration configuration)
        {
            string dbConnStr = configuration.GetConnectionString("Local_Database_Postgres")!;

            // 1. DATABASES
            services.AddDbContext<CmsDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions =>npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "account")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService(sp => new CmsFilesSeederHostedService(publicFilesPath));

            // 3. SERVICES
            services.AddScoped<PublicFileService, PublicFileService>();
        
            // 4. EXCEPTION HANDLERS
            services.AddExceptionHandler<CmsExceptionHandler>();
            services.AddProblemDetails();

            // 5. FLUID VALIDATORS
            
            return services;
        }
        public static void AddCmsModule(WebApplication app)
        {
            
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(publicFilesPath),
                RequestPath = "/api/koala/content"
            });
        }
    }
}