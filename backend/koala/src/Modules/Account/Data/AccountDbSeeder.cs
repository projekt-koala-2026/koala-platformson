using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using koala.src.Modules.Account.Entities;

using koala.src.Shared;

namespace koala.src.Modules.Account.Data
{
    public class AccountDbSeederHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public AccountDbSeederHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            bool hasAdmin = await dbContext.UserRoles
                .Join(dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .AnyAsync(roleName => roleName == "ORGANIZATION_ADMIN", cancellationToken);

            if (!hasAdmin)
            {
                var adminRoleId = Guid.Parse("01a027c5-d599-73de-bd5c-11f84a3fc125");
                var dummyAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin",
                    NameFirst = null,
                    NameLast = null,
                    PasswordHash = PasswordHasher.Hash("admin"), 
                    Verified = true,
                    AcceptedRodo = true,
                    AcceptedRules = true,
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