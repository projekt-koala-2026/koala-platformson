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

        public FileService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
            _publicFilesPath = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_PATH");
        }

        public async Task<FileInfoVM> SavePublicFile(string fileTitle, IFormFile file)
        {
            using var context = await _factory.CreateDbContextAsync();

            Guid newId = Guid.NewGuid(); 
            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{newId}{extension}";
            string publicUrl = $"/content/{fileName}";
        
            var filePath = Path.Combine(_publicFilesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        
            // Save metadata to DB
            var newFile = new PublicFile
            {
                Id = newId,
                Title = fileTitle,
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


        public async Task<List<FileInfoVM>> ListPublicFiles()
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            using var context = await _factory.CreateDbContextAsync();

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