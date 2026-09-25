using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Services;
using koala.src.Shared.Account;

namespace koala.src.Modules.Account
{
    public static class AccountModuleExtensions
    {
        public static IServiceCollection AddAccountModule(this IServiceCollection services, IConfiguration configuration)
        {
            string dbConnStr = configuration.GetConnectionString("Local_Database_Postgres")!;
            string privateFilesPath = configuration["FileSettings:PrivateFilesPath"]!;

            // 1. DATABASES
            services.AddDbContext<AccountDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions =>npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "account")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService<AccountSeederHostedService>();

            // 3. SERVICES
            services.AddScoped<EmailService, EmailService>();
            services.AddScoped<IAccountModule, AccountService>();
            services.AddScoped<SessionService, SessionService>();
            services.AddScoped<UserService, UserService>();
            services.AddScoped<LinkService, LinkService>();
            services.AddScoped<TeamService, TeamService>();
            services.AddScoped<RodoService, RodoService>(sp => {
                var db = sp.GetRequiredService<AccountDbContext>();
                return new RodoService(db, privateFilesPath);
            });
        
            // 4. EXCEPTION HANDLERS
            services.AddExceptionHandler<AccountExceptionHandler>();
            services.AddProblemDetails();

            // 5. FLUID VALIDATORS
            
            return services;
        }
    }
}