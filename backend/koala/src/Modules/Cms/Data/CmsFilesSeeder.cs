using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace koala.src.Modules.Cms.Data
{
    public class CmsFilesSeederHostedService : IHostedService
    {
        private readonly string _publicFilesPath;
        public CmsFilesSeederHostedService(string publicFilesPath)
        {
            _publicFilesPath = publicFilesPath;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_publicFilesPath))
            {
                Directory.CreateDirectory(_publicFilesPath);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}