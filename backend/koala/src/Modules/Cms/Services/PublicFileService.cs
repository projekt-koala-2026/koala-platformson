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

        public async Task<PublicFileDto> AddFileAsync(ClaimsPrincipal? claimsPrincipal, CreatePublicFileRequestDto createPublicFileRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(KoalaErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            try
            {
                Directory.CreateDirectory(_publicFilesPath);
            }
            catch
            {
                throw new CmsException(CmsErrorCodes.FolderCreationError, "Error when creating a folder holding public files");    
            }

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(createPublicFileRequestDto.File.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_publicFilesPath, path);
            Console.WriteLine(_publicFilesPath);
            Console.WriteLine(id);
            Console.WriteLine(extension);
            Console.WriteLine(path);
            Console.WriteLine(physicalPath);
            
            try
            {       
                await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
                {
                    await createPublicFileRequestDto.File.CopyToAsync(stream);
                }
            }
            catch
            {
                throw new CmsException(CmsErrorCodes.FileCreationError, "Error creating a public file");
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
                publicFile.Version = 0;

                await _db.PublicFiles.AddAsync(publicFile);
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw new CmsException(CmsErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            return new PublicFileDto(publicFile.Id, publicFile.Name, publicFile.Path, publicFile.Type, publicFile.CreatedAt, publicFile.Version);
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

        public async Task<(List<PublicFileDto>, ApiPagination)> GetFilesAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, PublicFileQueryDto publicFileQueryDto)
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

            var query = _db.PublicFiles.AsNoTracking().AsQueryable();

            if(!string.IsNullOrEmpty(publicFileQueryDto.Type))
            {
                query = query.Where(pf => pf.Type == publicFileQueryDto.Type);
            }

            if(!string.IsNullOrEmpty(publicFileQueryDto.Name))
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