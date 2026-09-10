using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Security;

namespace koala.src.Modules.Cms.Services
{
    public class PublicFileService
    {
        private readonly CmsDbContext _db;
        private readonly string _publicFilesPath;
        public PublicFileService(CmsDbContext db, string publicFilesPath)
        {
            _db = db;
            _publicFilesPath = publicFilesPath;
        }

        public async Task<PublicFileDto> AddFileAsync(ClaimsPrincipal? claimsPrincipal,string name,IFormFile file)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            Directory.CreateDirectory(_publicFilesPath);

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(file.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_publicFilesPath, path);

            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                DateTime timeNow = DateTime.UtcNow;
                PublicFile publicFile = new PublicFile
                {
                    Id = id,
                    Name = name,
                    Path = path,
                    Type = extension,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 0
                };

                await _db.PublicFiles.AddAsync(publicFile);

                return new PublicFileDto(publicFile.Id,publicFile.Name,publicFile.Path,publicFile.Type,publicFile.CreatedAt,publicFile.Version);
            }
            catch
            {
                File.Delete(physicalPath);
                throw;
            }
        }

        public async Task DeleteFileAsync(ClaimsPrincipal? claimsPrincipal,Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            var publicFile = await _db.PublicFiles.FirstOrDefaultAsync(pf => pf.Id == id);

            if(publicFile == null)
            {
                throw new CmsException(CmsErrorCodes.FileNotFound, "Could not found such file");
            }

            _db.PublicFiles.Remove(publicFile);
            await _db.SaveChangesAsync();

            var physicalPath = Path.Combine(_publicFilesPath, publicFile.Path);

            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                throw;
            }
        }

        public async Task<(List<PublicFileDto>, ApiPagination)> GetFilesAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            var publicFiles = await _db.PublicFiles
                .AsNoTracking()
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
                .ToListAsync();

            return (publicFiles, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, await _db.PublicFiles.AsNoTracking().CountAsync()));
        }
    }
}