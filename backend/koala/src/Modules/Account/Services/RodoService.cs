using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;

namespace koala.src.Modules.Account.Services
{
    public interface IRodoService
    {
        public Task<TeamMemberDto> AddRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoCreateDto);
        public Task<TeamMemberDto> UpdateRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoUpdateDto);
        public Task DeleteRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<TeamMemberDto> GetRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<string> GetRodoFileAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<(List<TeamMemberDto>, ApiPagination)> GetRodosAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, PageQueryDto pageQueryDto);
        public Task<TeamMemberDto> SetRodoStateAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoStateUpdateDto rodoStateUpdateDto);
    }
    public class RodoService : IRodoService
    {
        private readonly string _privateFilesPath;
        private readonly AccountDbContext _db;

        public RodoService(AccountDbContext db, string privateFilesPath)
        {
            _db = db;
            _privateFilesPath = privateFilesPath;
        }
        public async Task<TeamMemberDto> AddRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoCreateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }


            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId == null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoAlreadyExists, "can only add a rodo for a existing team member that does not have a rodo");
            }

            try
            {
                Directory.CreateDirectory(_privateFilesPath);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FolderCreationFailure, "Error when creating a folder holding private files");
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
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
            }

            Rodo rodo = new Rodo();
            try
            {
                DateTime timeNow = DateTime.UtcNow;
                rodo.Id = id;
                rodo.Type = extension;
                rodo.CreatedAt = timeNow;
                rodo.UpdatedAt = timeNow;
                rodo.Version = 1;

                teamMember.RodoId = rodo.Id;
                teamMember.RodoState = TeamMemberRodoState.RODOSTATE_UNVERIFIED;

                await _db.Rodos.AddAsync(rodo);
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving public file record to database");
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
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Error reading the saved file from disk");
            }

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo.Version,
                        teamMember.Rodo.CreatedAt,
                        teamMember.Rodo.UpdatedAt,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );

        }
        public async Task<TeamMemberDto> UpdateRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoUpdateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only update a rodo for a existing team member that has a rodo");
            }

            try
            {
                Directory.CreateDirectory(_privateFilesPath);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FolderCreationFailure, "Error when creating a folder holding private files");
            }

            if (teamMember.Rodo == null)
            {
                throw new KoalaException(KoalaErrorCodes.RodoNotFound, "Error could not found rodo");
            }

            if (rodoUpdateDto.Version == null)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Error request lack version");
            }

            if (teamMember.Rodo.Version != rodoUpdateDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Error make sure u have fresh data versions does not match");
            }

            var id = teamMember.RodoId;
            var extension = Path.GetExtension(rodoUpdateDto.File.FileName);
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_privateFilesPath, path);

            try
            {
                await using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await rodoUpdateDto.File.CopyToAsync(stream);
                }
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
            }

            try
            {
                DateTime timeNow = DateTime.UtcNow;
                teamMember.Rodo.Type = extension;
                teamMember.Rodo.UpdatedAt = timeNow;
                teamMember.RodoState = TeamMemberRodoState.RODOSTATE_UNVERIFIED;

                await _db.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo.Version,
                        teamMember.Rodo.CreatedAt,
                        teamMember.Rodo.UpdatedAt,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );
        }
        public async Task DeleteRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only delete a rodo for a existing team member that has a rodo");
            }

            if (teamMember.Rodo == null)
            {
                throw new KoalaException(KoalaErrorCodes.RodoNotFound, "Error could not found rodo");
            }

            var physicalPath = Path.Combine(_privateFilesPath, $"{teamMember.RodoId}{teamMember.Rodo.Type}");

            try
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Error deleting the physical file from disk");
            }

            _db.Rodos.Remove(teamMember.Rodo);
            await _db.SaveChangesAsync();
        }
        public async Task<TeamMemberDto> GetRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only delete a rodo for a existing team member that has a rodo");
            }

            if (teamMember.Rodo == null)
            {
                throw new KoalaException(KoalaErrorCodes.RodoNotFound, "Error could not found rodo");
            }

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo.Version,
                        teamMember.Rodo.CreatedAt,
                        teamMember.Rodo.UpdatedAt,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );
        }
        public async Task<string> GetRodoFileAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only delete a rodo for a existing team member that has a rodo");
            }

            if (teamMember.Rodo == null)
            {
                throw new KoalaException(KoalaErrorCodes.RodoNotFound, "Error could not found rodo");
            }

            return Path.Combine(_privateFilesPath, $"{teamMember.RodoId}{teamMember.Rodo.Type}");
        }
        public async Task<(List<TeamMemberDto>, ApiPagination)> GetRodosAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin that is a member of this team and organization admin can view the rodos");
            }

            var query = _db.TeamMembers.AsNoTracking().AsQueryable();

            query = query.Where(tm => tm.TeamId == teamId);

            var queryResults = await query.ToListAsync();
            var rodos = queryResults
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(tm => new TeamMemberDto
                    (
                        tm.Id,
                        tm.TeamId,
                        tm.NameFirst == null ? tm.Team.Captain.NameFirst! : tm.NameFirst!,
                        tm.NameLast == null ? tm.Team.Captain.NameLast! : tm.NameLast!,
                        tm.RodoState,
                        tm.Rodo.Version,
                        tm.Rodo.CreatedAt,
                        tm.Rodo.UpdatedAt,
                        tm.CreatedAt,
                        tm.UpdatedAt,
                        tm.Version
                    )).ToList();

            return (rodos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
        public async Task<TeamMemberDto> SetRodoStateAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoStateUpdateDto rodoStateUpdateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin and organization admin can view the rodos");
            }

            var teamMember = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only chamge state for an existing team member that has a rodo");
            }

            teamMember.RodoState = rodoStateUpdateDto.State;
            await _db.SaveChangesAsync();

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo.Version,
                        teamMember.Rodo.CreatedAt,
                        teamMember.Rodo.UpdatedAt,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );
        }
    }
}