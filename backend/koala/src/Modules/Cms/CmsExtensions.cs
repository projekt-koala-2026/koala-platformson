using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Services;
using Microsoft.Extensions.FileProviders;
using koala.src.Modules.Cms.Entities;


namespace koala.src.Modules.Cms
{
    public static class CmsModuleExtensions
    {
        public static IServiceCollection AddCmsModule(this IServiceCollection services, IConfiguration configuration)
        {
            string dbConnStr = configuration.GetConnectionString("Local_Database_Postgres")!;
            string publicFilesPath = configuration["FileSettings:PublicFilesPath"]!;

            // 1. DATABASES
            services.AddDbContext<CmsDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions =>npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "cms")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService(sp => new CmsSeederHostedService(publicFilesPath, sp));

            // 3. SERVICES
            services.AddScoped<KoalicjantService, KoalicjantService>();
            services.AddScoped<PostService, PostService>();
            services.AddScoped<PublicFileService, PublicFileService>(sp => {
                var db = sp.GetRequiredService<CmsDbContext>();
                return new PublicFileService(db, publicFilesPath);
            });
            services.AddScoped<SponsorService, SponsorService>();
            services.AddScoped<StaticPageService, StaticPageService>();

            // 4. EXCEPTION HANDLERS
            services.AddExceptionHandler<CmsExceptionHandler>();
            services.AddProblemDetails();

            // 5. FLUID VALIDATORS
            
            return services;
        }
        public static void AddCmsModule(WebApplication app, IConfiguration configuration)
        {
            string publicFilesPath = configuration["FileSettings:PublicFilesPath"]!;
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(publicFilesPath),
                RequestPath = "/api/koala/content"
            });
        }
    }
}