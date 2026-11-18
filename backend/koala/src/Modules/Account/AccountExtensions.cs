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

            string admin_email = configuration["DefaultAdmin:Email"]!;
            string admin_passw = configuration["DefaultAdmin:Password"]!;

            // 1. DATABASES
            services.AddDbContext<AccountDbContext>(options =>
                options.UseNpgsql
                (
                    dbConnStr,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "account")
                ));

            // 2. HOSTED SERVICES (RUNING ON BACKEND START)
            services.AddHostedService<AccountSeederHostedService>(sp =>
            {
                return new AccountSeederHostedService(sp, admin_email, admin_passw);
            });

            // 3. SERVICES
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAccountModule, AccountService>(sp =>
            {
                var db = sp.GetRequiredService<AccountDbContext>();
                var cache = sp.GetRequiredService<ICacheService>();
                return new AccountService(db, cache, privateFilesPath);
            });
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILinkService, LinkService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IRodoService, RodoService>(sp =>
            {
                var db = sp.GetRequiredService<AccountDbContext>();
                return new RodoService(db, privateFilesPath);
            });

            // 4. FLUID VALIDATORS
            services.AddValidatorsFromAssemblyContaining<Program>();

            return services;
        }
    }
}