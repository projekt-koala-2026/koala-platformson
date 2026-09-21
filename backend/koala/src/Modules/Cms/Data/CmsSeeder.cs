using koala.src.Modules.Cms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace koala.src.Modules.Cms.Data
{
    public class CmsSeederHostedService : IHostedService
    {
        private readonly string _publicFilesPath;
        private readonly IServiceProvider _serviceProvider;
        public CmsSeederHostedService(string publicFilesPath, IServiceProvider serviceProvider)
        {
            _publicFilesPath = publicFilesPath;
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS cms;");
            await dbContext.Database.MigrateAsync();


            if (!Directory.Exists(_publicFilesPath))
            {
                Directory.CreateDirectory(_publicFilesPath);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}