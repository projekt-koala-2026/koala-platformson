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
        void Move(string source_path, string destination_path, bool overwrite);
        Task WriteAsync(string path, IFormFile file, bool overwrite);
        Task<byte[]> ReadAllBytesAsync(string path);
        bool Exists(string path);
        void Delete(string path);
    }

    internal sealed class PhysicalPublicFileOperations : IPublicFileOperations
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public async Task WriteAsync(string path, IFormFile file, bool overwrite)
        {
            await using var stream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }

        public Task<byte[]> ReadAllBytesAsync(string path) => File.ReadAllBytesAsync(path);
        public bool Exists(string path) => File.Exists(path);
        public void Delete(string path) => File.Delete(path);
        public void Move(string source_path, string destination_path, bool overwrite) => File.Move(source_path, destination_path, overwrite);
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

        private void TryDeletePhysicalFile(string path)
        {
            try
            {
                if (_fileOperations.Exists(path))
                {
                    _fileOperations.Delete(path);
                }
            }
            catch
            {
                // Preserve the original database error if best-effort file cleanup also fails.
            }
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
                await _fileOperations.WriteAsync(physicalPath, createPublicFileRequestDto.File, overwrite: true);
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
                publicFile.Description = createPublicFileRequestDto.Description; 
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

            return new PublicFileDto(publicFile.Id, publicFile.Name, publicFile.Description, publicFile.Path, publicFile.Type, publicFile.CreatedAt, publicFile.UpdatedAt, publicFile.Version);
        }

        public async Task<PublicFileDto> UpdateFileMetaDataAsync(ClaimsPrincipal? claimsPrincipal, Guid file_id, UpdatePublicFileRequestDto requestDto)
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

            var file = await _db.PublicFiles.FirstOrDefaultAsync(pf => pf.Id == file_id);

            if(file == null)
            {
                throw new KoalaException(KoalaErrorCodes.PublicFileNotFound, "Public file does not exist");
            }

            file.Name = requestDto.Name;
            file.Description = requestDto.Description;
            file.UpdatedAt = DateTime.UtcNow;
            file.Version++;

            try
            {
                await _db.SaveChangesAsync();
            }catch
            {
                
            }

            return new PublicFileDto
                (
                    file.Id,
                    file.Name,
                    file.Description,
                    file.Path,
                    file.Type,
                    file.CreatedAt,
                    file.UpdatedAt,
                    file.Version
                );
        }

        public async Task<PublicFileDto> UpdateFileAsync(ClaimsPrincipal? claimsPrincipal, Guid file_id, CreatePublicFileRequestDto requestDto)
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

            var file = await _db.PublicFiles.FirstOrDefaultAsync(pf => pf.Id == file_id);

            if(file == null)
            {
                throw new KoalaException(KoalaErrorCodes.PublicFileNotFound, "Public file does not exist");
            }

            var temp_id = Guid.CreateVersion7();
            var id = file.Id;
            var oldExtension = file.Type;
            var newExtension = Path.GetExtension(requestDto.File.FileName);

            var oldPhysicalPath = Path.Combine(_publicFilesPath, $"{id}{oldExtension}");
            var newPath = $"{id}{newExtension}";
            var newPhysicalPath = Path.Combine(_publicFilesPath, $"{id}{newExtension}");
            var tempPhysicalPath = Path.Combine(_publicFilesPath, $"{id}_{temp_id}.tmp");

            try
            {
                await _fileOperations.WriteAsync(tempPhysicalPath, requestDto.File, overwrite: true);
            }
            catch
            {
                TryDeletePhysicalFile(tempPhysicalPath);
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
            }

            try
            {
                DateTime timeNow = DateTime.UtcNow;
                file.Name = requestDto.Name;
                file.Description = requestDto.Description;
                file.Path = newPath;
                file.Type = newExtension;
                file.UpdatedAt = timeNow;
                file.Version++;

                await _db.SaveChangesAsync();
            }
            catch
            {
                TryDeletePhysicalFile(tempPhysicalPath);
                throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving private file record to database");
            }

            try
            {
                _fileOperations.Move(tempPhysicalPath, newPhysicalPath, overwrite: true);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error finalizing private file update");
            }

            if (!string.Equals(oldExtension, newExtension, StringComparison.OrdinalIgnoreCase))
            {
                TryDeletePhysicalFile(oldPhysicalPath);
            }

            return new PublicFileDto
                (
                    file.Id,
                    file.Name,
                    file.Description,
                    file.Path,
                    file.Type,
                    file.CreatedAt,
                    file.UpdatedAt,
                    file.Version
                );
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
                    pf.Description,
                    pf.Path,
                    pf.Type,
                    pf.CreatedAt,
                    pf.UpdatedAt,
                    pf.Version
                ))
                .ToList();

            return (publicFiles, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
    }
}
