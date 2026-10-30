using Microsoft.EntityFrameworkCore;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Services;
using koala.src.Shared.Account;
using FluentValidation;

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
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "account")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService<AccountSeederHostedService>();

            // 3. SERVICES
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAccountModule, AccountService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILinkService, LinkService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IRodoService, RodoService>(sp =>
            {
                var db = sp.GetRequiredService<AccountDbContext>();
                return new RodoService(db, privateFilesPath);
            });

            // 4. EXCEPTION HANDLERS
            services.AddExceptionHandler<AccountExceptionHandler>();
            services.AddProblemDetails();

            // 5. FLUID VALIDATORS
            services.AddValidatorsFromAssemblyContaining<Program>();

            return services;
        }
    }
}