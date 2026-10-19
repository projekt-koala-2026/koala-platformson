using koala.src.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace koala.src.Modules.Core.Data
{
    public class CoreSeederHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        public CoreSeederHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS core;");
            await dbContext.Database.MigrateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}