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
        public CmsSeederHostedService(string publicFilesPath)
        {
            _publicFilesPath = publicFilesPath;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {

            if (!Directory.Exists(_publicFilesPath))
            {
                Directory.CreateDirectory(_publicFilesPath);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}