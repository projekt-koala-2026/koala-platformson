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
    public class FileService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly string _publicFilesPath;
        private readonly List<string> _publicFilesFolders;

        public FileService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
            _publicFilesPath = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_PATH");
            _publicFilesFolders = new List<string>
            {
                "posts",
                "problems",
                "history",
                "rules",
                "images"
            };
        }

        public async Task CreateFolderStructure()
        {
            var basePath = _publicFilesPath;

            if (string.IsNullOrWhiteSpace(basePath))
                throw new Exception("PUBLIC_STORAGE_PATH is not set");

            foreach (var folder in _publicFilesFolders)
            {
                var fullPath = Path.Combine(basePath, folder);

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
            
            var rulesFile = Path.Combine(basePath, "rules/rules.json");
            if (!File.Exists(rulesFile))
            {
                await File.WriteAllTextAsync(rulesFile, string.Empty);
            }

            var historyFile = Path.Combine(basePath, "history/history.json");
            if (!File.Exists(historyFile))
            {
                await File.WriteAllTextAsync(historyFile, string.Empty);
            }

            var problemsFile = Path.Combine(basePath, "problems/problems.json");
            if (!File.Exists(problemsFile))
            {
                await File.WriteAllTextAsync(problemsFile, string.Empty);
            }

            await Task.CompletedTask;
        }

        public async Task<FileInfoVM> SavePublicFile(string folder, string fileTitle, IFormFile file)
        {
            if(!_publicFilesFolders.Contains(folder))
            {
                return null;
            }

            using var context = await _factory.CreateDbContextAsync();

            Guid newId = Guid.NewGuid(); 
            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{newId}{extension}";
            string publicUrl = $"/content/{folder}/{fileName}";
        
            var filePath = Path.Combine(_publicFilesPath, folder);
            filePath = Path.Combine(filePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        
            // Save metadata to DB
            var newFile = new PublicFile
            {
                Id = newId,
                Title = fileTitle,
                Folder = folder,
                FilePath = publicUrl
            };
        
            context.PublicFiles.Add(newFile);
            await context.SaveChangesAsync();

            var fileInfoVM = new FileInfoVM
            {
                Id = newFile.Id,
                Title = newFile.Title,
                FilePath = newFile.FilePath
            };

            return fileInfoVM;
        }

        public async Task<List<FileInfoVM>> ListPublicFiles(FileGetVM getFile)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            using var context = await _factory.CreateDbContextAsync();

            if(string.IsNullOrWhiteSpace(getFile.Folder))
            {
                var fileInfoVMs = await context.PublicFiles
                .Select(file => new FileInfoVM
                {
                    Id = file.Id,
                    Title = file.Title,
                    FilePath = file.FilePath
                })
                .ToListAsync();

                return fileInfoVMs;
            }
            else
            {
                var fileInfoVMs = await context.PublicFiles
                .Where(file => file.Folder == getFile.Folder)
                .Select(file => new FileInfoVM
                {
                    Id = file.Id,
                    Title = file.Title,
                    FilePath = file.FilePath
                })
                .ToListAsync();

                return fileInfoVMs;
            }
        }

        public async Task DeletePublicFile(Guid Id)
        {
            using var context = await _factory.CreateDbContextAsync();

            var file = await context.PublicFiles
                .FirstOrDefaultAsync(f => f.Id == Id);

            if (file == null)
                return;

            var fileName = Path.GetFileName(file.FilePath);
            var fullPath = Path.Combine(_publicFilesPath, fileName);

            // 1. usuń z dysku
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // 2. usuń z DB
            context.PublicFiles.Remove(file);
            await context.SaveChangesAsync();

            return;
        }
    }
}