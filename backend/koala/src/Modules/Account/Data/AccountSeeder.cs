using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using koala.src.Modules.Account.Entities;

using koala.src.Shared;

namespace koala.src.Modules.Account.Data
{
    public class AccountSeederHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _admin_passw;
        private readonly string _admin_email;
        public AccountSeederHostedService(IServiceProvider serviceProvider, string admin_email, string admin_passw)
        {
            _serviceProvider = serviceProvider;
            _admin_email = admin_email;
            _admin_passw = admin_passw;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS account;");
            await dbContext.Database.MigrateAsync();

            bool hasAdmin = await dbContext.UserRoles
                .Join(dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .AnyAsync(roleName => roleName == RoleNames.NAME_ORGANIZATION_ADMIN, cancellationToken);

            if (!hasAdmin)
            {
                var adminRoleId = Guid.Parse(AccountDbContext.static_id_guid_role_organization_admin);
                var dummyAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    EditionId = null,
                    Email = _admin_email,
                    NameFirst = "seeded",
                    NameLast = "seeded",
                    PasswordHash = PasswordHasher.Hash(_admin_passw),
                    Verified = true,
                    AcceptedRodo = true,
                    AcceptedRules = true,
                    DeclaredAtLeast18 = true,
                    CreatedAt = DateTime.UtcNow
                };

                var userRole = new UserRole
                {
                    UserId = dummyAdmin.Id,
                    RoleId = adminRoleId
                };

                dbContext.Users.Add(dummyAdmin);
                dbContext.UserRoles.Add(userRole);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}