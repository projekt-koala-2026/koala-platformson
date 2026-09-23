using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Account.Services
{
    public class RodoService
    {
        private readonly string _privateFilesPath;
        private readonly AccountDbContext _db;

        public RodoService(AccountDbContext db, string privateFilesPath)
        {
            _db = db;
            _privateFilesPath = privateFilesPath;
        }
        public async Task<RodoDto> AddRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoCreateDto rodoCreateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            bool isMemeber = await _db.TeamMembers.AsNoTracking().AnyAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);
            if(!isMemeber)
            {
                throw new AccountException(AccountErrorCodes.BadRequest,"can only add a rodo for a existing team member that does not have a rodo");
            }

            try
            {
                Directory.CreateDirectory(_privateFilesPath);
            }
            catch
            {
                throw new AccountException(AccountErrorCodes.FolderCreationError, "Error when creating a folder holding private files");    
            }

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(rodoCreateDto.File.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_privateFilesPath, path);
            
            try
            {       
                await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
                {
                    await rodoCreateDto.File.CopyToAsync(stream);
                }
            }
            catch
            {
                throw new AccountException(AccountErrorCodes.FileCreationError, "Error creating a private file");
            }

            Rodo rodo = new Rodo();
            try
            {
                DateTime timeNow = DateTime.UtcNow;
                rodo.Id = id;
                rodo.TeamId = teamId;
                rodo.UserId = teamMemberId;
                rodo.Type = extension;
                rodo.State = "UNVERIFIED";
                rodo.CreatedAt = timeNow;
                rodo.UpdatedAt = timeNow;

                await _db.Rodos.AddAsync(rodo);
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw new AccountException(AccountErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            IFormFile savedFile;
            try
            {
                var filePath = $"{rodo.Id}{rodo.Type}";
                var filePhysicalPath = Path.Combine(_privateFilesPath, filePath);
                var memoryStream = new MemoryStream();
                await using (var fileStream = new FileStream(filePhysicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }
                memoryStream.Position = 0;

                savedFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", Path.GetFileName(filePhysicalPath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = rodoCreateDto.File.ContentType
                };
            }
            catch (Exception)
            {
                throw new AccountException(AccountErrorCodes.FileNotFound, "Error reading the saved file from disk");
            }

            return new RodoDto(rodo.TeamId, rodo.UserId, rodo.State, rodo.CreatedAt, rodo.UpdatedAt);

        }
        public async Task<RodoDto> UpdateRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpdateDto rodoUpdateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            bool isMemeber = await _db.TeamMembers.AsNoTracking().AnyAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);
            if(!isMemeber)
            {
                throw new AccountException(AccountErrorCodes.BadRequest,"can only add a rodo for a existing team member that does not have a rodo");
            }

            try
            {
                Directory.CreateDirectory(_privateFilesPath);
            }
            catch
            {
                throw new AccountException(AccountErrorCodes.FolderCreationError, "Error when creating a folder holding private files");    
            }

            Rodo? rodo = await _db.Rodos.FirstOrDefaultAsync(r => r.TeamId == teamId && r.UserId == teamMemberId);

            if(rodo == null)
            {
                throw new AccountException(AccountErrorCodes.RodoNotFound, "Error could not found rodo");    
            }

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(rodoUpdateDto.File.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_privateFilesPath, path);
            
            try
            {       
                await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
                {
                    await rodoUpdateDto.File.CopyToAsync(stream);
                }
            }
            catch
            {
                throw new AccountException(AccountErrorCodes.FileCreationError, "Error creating a private file");
            }

            try
            {
                DateTime timeNow = DateTime.UtcNow;
                rodo.Id = id;
                rodo.TeamId = teamId;
                rodo.UserId = teamMemberId;
                rodo.Type = extension;
                rodo.State = "UNVERIFIED";
                rodo.UpdatedAt = timeNow;

                await _db.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw new AccountException(AccountErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            return new RodoDto(rodo.TeamId, rodo.UserId, rodo.State, rodo.CreatedAt, rodo.UpdatedAt);
        }
        public async Task DeleteRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            bool isMemeber = await _db.TeamMembers.AsNoTracking().AnyAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);
            if(!isMemeber)
            {
                throw new AccountException(AccountErrorCodes.BadRequest,"can only add a rodo for a existing team member that does not have a rodo");
            }

            Rodo? rodo = await _db.Rodos.FirstOrDefaultAsync(r => r.TeamId == teamId && r.UserId == teamMemberId);

            if(rodo == null)
            {
                throw new AccountException(AccountErrorCodes.RodoNotFound, "Error could not found rodo");    
            }

            var physicalPath = Path.Combine(_privateFilesPath, $"{rodo.Id}{rodo.Type}");

            try
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch
            {
                throw new AccountException(AccountErrorCodes.FileNotFound, "Error deleting the physical file from disk");
            }

            _db.Rodos.Remove(rodo);
            await _db.SaveChangesAsync();
        }
        public async Task<RodoDto> GetRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            bool isMemeber = await _db.TeamMembers.AsNoTracking().AnyAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);
            if(!isMemeber && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.BadRequest,"can only add a rodo for a existing team member that does not have a rodo");
            }

            Rodo? rodo = await _db.Rodos.FirstOrDefaultAsync(r => r.TeamId == teamId && r.UserId == teamMemberId);

            if(rodo == null)
            {
                throw new AccountException(AccountErrorCodes.RodoNotFound, "Error could not found rodo");    
            }

            return new RodoDto(rodo.TeamId, rodo.UserId, rodo.State, rodo.CreatedAt, rodo.UpdatedAt);
        }
        public async Task<string> GetRodoFileAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            bool isMemeber = await _db.TeamMembers.AsNoTracking().AnyAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);
            if(!isMemeber && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.BadRequest,"can only add a rodo for a existing team member that does not have a rodo");
            }

            Rodo? rodo = await _db.Rodos.FirstOrDefaultAsync(r => r.TeamId == teamId && r.UserId == teamMemberId);

            if(rodo == null)
            {
                throw new AccountException(AccountErrorCodes.RodoNotFound, "Error could not found rodo");    
            }

            return Path.Combine(_privateFilesPath, $"{rodo.Id}{rodo.Type}");
        }
        public async Task<(List<RodoDto>, ApiPagination)> GetRodosAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamMember = await _db.TeamMembers.AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isUserTeamMember && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin that is a member of this tean and organization admin can view the rodos");
            }

            var query = _db.Rodos.AsNoTracking().AsQueryable();

            query = query.Where(r => r.TeamId == teamId);

            var queryResults = await query.ToListAsync();
            var rodos = queryResults
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(r => new RodoDto(r.TeamId,r.UserId,r.State,r.CreatedAt,r.UpdatedAt)).ToList();

            return (rodos, new ApiPagination(pageQueryDto.PageNumber,pageQueryDto.PageSize, queryResults.Count)); 
        }
        public async Task<RodoDto> SetRodoStateAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoStateUpdateDto rodoStateUpdateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team admin and organization admin can view the rodos");
            }

            Rodo? rodo = await _db.Rodos.FirstOrDefaultAsync(r => r.TeamId == teamId && r.UserId == teamMemberId);

            if(rodo == null)
            {
                throw new AccountException(AccountErrorCodes.RodoNotFound, "Error could not found rodo");    
            }

            rodo.State = rodoStateUpdateDto.State;
            await _db.SaveChangesAsync();

            return new RodoDto(rodo.TeamId, rodo.UserId, rodo.State, rodo.CreatedAt, rodo.UpdatedAt);
        }
    }
}