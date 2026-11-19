using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public interface IPublicFileOperations
    {
        void CreateDirectory(string path);
        Task WriteAsync(string path, Microsoft.AspNetCore.Http.IFormFile file);
        bool Exists(string path);
        void Delete(string path);
    }

    internal sealed class PhysicalPublicFileOperations : IPublicFileOperations
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public async Task WriteAsync(string path, Microsoft.AspNetCore.Http.IFormFile file)
        {
            await using var stream = new FileStream(path, FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }

        public bool Exists(string path) => File.Exists(path);
        public void Delete(string path) => File.Delete(path);
    }

    public class PublicFileService
    {
        private readonly CmsDbContext _db;
        private readonly string _publicFilesPath;
        private readonly IPublicFileOperations _fileOperations;
        public PublicFileService(CmsDbContext db, string publicFilesPath)
            : this(db, publicFilesPath, new PhysicalPublicFileOperations())
        {
        }

        public PublicFileService(CmsDbContext db, string publicFilesPath, IPublicFileOperations fileOperations)
        {
            _db = db;
            _publicFilesPath = publicFilesPath;
            _fileOperations = fileOperations;
        }

        public async Task<PublicFileDto> AddFileAsync(ClaimsPrincipal? claimsPrincipal, CreatePublicFileRequestDto createPublicFileRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            try
            {
                _fileOperations.CreateDirectory(_publicFilesPath);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FolderCreationFailure, "Error when creating a folder holding public files");
            }

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(createPublicFileRequestDto.File.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_publicFilesPath, path);
            try
            {
                await _fileOperations.WriteAsync(physicalPath, createPublicFileRequestDto.File);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a public file");
            }

            PublicFile publicFile = new PublicFile();
            try
            {
                DateTime timeNow = DateTime.UtcNow;
                publicFile.Id = id;
                publicFile.Name = createPublicFileRequestDto.Name;
                publicFile.Path = path;
                publicFile.Type = extension;
                publicFile.CreatedAt = timeNow;
                publicFile.UpdatedAt = timeNow;
                publicFile.Version = 1;

                await _db.PublicFiles.AddAsync(publicFile);
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (_fileOperations.Exists(physicalPath))
                {
                    _fileOperations.Delete(physicalPath);
                }

                throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            return new PublicFileDto(publicFile.Id, publicFile.Name, publicFile.Path, publicFile.Type, publicFile.CreatedAt, publicFile.Version);
        }

        public async Task DeleteFileAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var publicFile = await _db.PublicFiles.FirstOrDefaultAsync(pf => pf.Id == id);

            if (publicFile == null)
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Could not find such file");
            }

            var physicalPath = Path.Combine(_publicFilesPath, publicFile.Path);

            try
            {
                _fileOperations.Delete(physicalPath);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileDeleteFailure, "Could not delete a specified file");
            }

            _db.PublicFiles.Remove(publicFile);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<PublicFileDto>, ApiPagination)> GetFilesAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, PublicFileQueryDto publicFileQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var query = _db.PublicFiles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(publicFileQueryDto.Type))
            {
                query = query.Where(pf => pf.Type == publicFileQueryDto.Type);
            }

            if (!string.IsNullOrEmpty(publicFileQueryDto.Name))
            {
                query = query.Where(pf => pf.Name == publicFileQueryDto.Name);
            }

            var queryResults = await query.ToListAsync();
            var publicFiles = queryResults
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(pf => new PublicFileDto
                (
                    pf.Id,
                    pf.Name,
                    pf.Path,
                    pf.Type,
                    pf.CreatedAt,
                    pf.Version
                ))
                .ToList();

            return (publicFiles, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
    }
}
