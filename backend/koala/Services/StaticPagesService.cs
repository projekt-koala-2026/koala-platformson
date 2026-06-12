
using koala.Data;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace koala.Services
{
    //FIXME: DECIDE ON THE ENDPOINT STYLE (POST, PUT, GET OR DELETE) !!!
    //FIXME: MAKE SURE QUERIES ARE OPTIMAL AND AS SPECIFIC AS POSSIBLE !!!
    public class StaticPagesService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly string _publicFilesPath;

        public StaticPagesService(IDbContextFactory<AppDbContext> factory)
        {
            //NOTE: MAYBE KEEP IN CASE WE WANT TO VERSION THOSE CHANGES IN DB?
            _factory = factory;
            _publicFilesPath = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_PATH");
        }

        public async Task<StaticPageInfoVM> UpdateRules(string content)
        {
            string path = Path.Combine(_publicFilesPath,"rules/rules.json");

            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            await File.WriteAllTextAsync(path, content);

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = content};
            return content;
        }

        public async Task<StaticPageInfoVM> UpdateHistory(string content)
        {
            string path = Path.Combine(_publicFilesPath,"history/history.json");

            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            await File.WriteAllTextAsync(path, content);

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = content};
            return content;
        }

        public async Task<StaticPageInfoVM> UpdateProblems(string content)
        {
            string path = Path.Combine(_publicFilesPath,"problems/problems.json");

            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            await File.WriteAllTextAsync(path, content);

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = content};
            return content;
        }

        public async Task<StaticPageInfoVM> GetRules()
        {
            string path = Path.Combine(_publicFilesPath,"rules/rules.json");

            if (!File.Exists(path))
            {
                return null;
            }

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = await File.ReadAllTextAsync(path)};
            return result.MarkdownBody;
        }

        public async Task<StaticPageInfoVM> GetHistory()
        {
            string path = Path.Combine(_publicFilesPath,"history/history.json");

            if (!File.Exists(path))
            {
                return null;
            }

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = await File.ReadAllTextAsync(path)};
            return result.MarkdownBody;
        }

        public async Task<StaticPageInfoVM> GetProblems()
        {
            string path = Path.Combine(_publicFilesPath,"problems/problems.json");

            if (!File.Exists(path))
            {
                return null;
            }

            StaticPageInfoVM result = new StaticPageInfoVM { MarkdownBody = await File.ReadAllTextAsync(path)};
            return result.MarkdownBody;
        }
    }
}
