using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Account.Services
{
    public interface IRodoFileOperations
    {
        void CreateDirectory(string path);
        void Move(string source_path, string destination_path, bool overwrite);
        Task WriteAsync(string path, IFormFile file, bool overwrite);
        Task<byte[]> ReadAllBytesAsync(string path);
        bool Exists(string path);
        void Delete(string path);
    }

    internal sealed class PhysicalRodoFileOperations : IRodoFileOperations
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

    public interface IRodoService
    {
        public Task<TeamMemberDto> AddRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoCreateDto);
        public Task<TeamMemberDto> UpdateRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoUpdateDto);
        public Task DeleteRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<TeamMemberDto> GetRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<string> GetRodoFileAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<(List<TeamMemberDto>, ApiPagination)> GetRodosAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, PageQueryDto pageQueryDto);
        public Task<TeamMemberDto> SetRodoStateAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoStateUpdateDto rodoStateUpdateDto);
        public Task INTERNAL_DeleteRodoPhysicalFile(Guid teamId, Guid teamMemberId);
    }
    public class RodoService : IRodoService
    {
        private readonly string _privateFilesPath;
        private readonly AccountDbContext _db;
        private readonly IRodoFileOperations _fileOperations;

        public RodoService(AccountDbContext db, string privateFilesPath)
            : this(db, privateFilesPath, new PhysicalRodoFileOperations())
        {
        }

        public RodoService(AccountDbContext db, string privateFilesPath, IRodoFileOperations fileOperations)
        {
            _db = db;
            _privateFilesPath = privateFilesPath;
            _fileOperations = fileOperations;
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
        public async Task<TeamMemberDto> AddRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoCreateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team's guardian can manage RODO documents");
            }


            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the guardian of this team can manage its RODO documents");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).Include(tm => tm.Team).ThenInclude(t => t.Captain).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId == null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoAlreadyExists, "can only add a rodo for a existing team member that does not have a rodo");
            }

            try
            {
                _fileOperations.CreateDirectory(_privateFilesPath);
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
                await _fileOperations.WriteAsync(physicalPath, rodoCreateDto.File, overwrite: false);
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
                TryDeletePhysicalFile(physicalPath);
                throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving public file record to database");
            }

            IFormFile savedFile;
            try
            {
                var filePath = $"{rodo.Id}{rodo.Type}";
                var filePhysicalPath = Path.Combine(_privateFilesPath, filePath);
                var memoryStream = new MemoryStream(await _fileOperations.ReadAllBytesAsync(filePhysicalPath));
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
                        teamMember.Rodo?.Version ?? null,
                        teamMember.Rodo?.CreatedAt ?? null,
                        teamMember.Rodo?.UpdatedAt ?? null,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );

        }

        //FIXME: ADD TRANSACTION HERE AND HANDLE FILE SAVINGS IN CONCURENCY USING TEMP FILES!!!
        public async Task<TeamMemberDto> UpdateRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, RodoUpsertDto rodoUpdateDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team's guardian can manage RODO documents");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the guardian of this team can manage its RODO documents");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).Include(tm => tm.Team).ThenInclude(t => t.Captain).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "can only update a rodo for a existing team member that has a rodo");
            }

            try
            {
                _fileOperations.CreateDirectory(_privateFilesPath);
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

            var temp_id = Guid.CreateVersion7();
            var id = teamMember.RodoId;
            var oldExtension = teamMember.Rodo.Type;
            var newExtension = Path.GetExtension(rodoUpdateDto.File.FileName);

            var oldPhysicalPath = Path.Combine(_privateFilesPath, $"{id}{oldExtension}");
            var newPhysicalPath = Path.Combine(_privateFilesPath, $"{id}{newExtension}");
            var tempPhysicalPath = Path.Combine(_privateFilesPath, $"{id}_{temp_id}.tmp");

            try
            {
                await _fileOperations.WriteAsync(tempPhysicalPath, rodoUpdateDto.File, overwrite: true);
            }
            catch
            {
                TryDeletePhysicalFile(tempPhysicalPath);
                throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
            }

            try
            {
                DateTime timeNow = DateTime.UtcNow;
                teamMember.Rodo.Type = newExtension;
                teamMember.Rodo.UpdatedAt = timeNow;
                teamMember.RodoState = TeamMemberRodoState.RODOSTATE_UNVERIFIED;
                teamMember.Rodo.Version++;

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

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo?.Version ?? null,
                        teamMember.Rodo?.CreatedAt ?? null,
                        teamMember.Rodo?.UpdatedAt ?? null,
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

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team's guardian can manage RODO documents");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the guardian of this team can manage its RODO documents");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
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
                if (_fileOperations.Exists(physicalPath))
                {
                    _fileOperations.Delete(physicalPath);
                }
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Error deleting the physical file from disk");
            }

            _db.Rodos.Remove(teamMember.Rodo);
            teamMember.RodoId = null;
            teamMember.RodoState = TeamMemberRodoState.RODOSTATE_NONE;
            teamMember.UpdatedAt = DateTime.UtcNow;
            teamMember.Version += 1;
            await _db.SaveChangesAsync();
        }

        public async Task INTERNAL_DeleteRodoPhysicalFile(Guid teamId, Guid teamMemberId)
        {
            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
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
                if (_fileOperations.Exists(physicalPath))
                {
                    _fileOperations.Delete(physicalPath);
                }
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Error deleting the physical file from disk");
            }
        }

        public async Task<TeamMemberDto> GetRodoAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            bool isOrganizationRodoModerator = ClaimsHelper.IsRoleOrganizationRodoModerator(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a team guardian or organization RODO moderator can view RODO documents");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only this team's guardian or an organization RODO moderator can view its RODO documents");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).Include(tm => tm.Team).ThenInclude(t => t.Captain).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberRodoNotFound, "Team member not found");
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
                        teamMember.Rodo?.Version ?? null,
                        teamMember.Rodo?.CreatedAt ?? null,
                        teamMember.Rodo?.UpdatedAt ?? null,
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

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            bool isOrganizationRodoModerator = ClaimsHelper.IsRoleOrganizationRodoModerator(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a team guardian or organization RODO moderator can view RODO documents");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only this team's guardian or an organization RODO moderator can view its RODO documents");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.RodoId != null);
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

            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            bool isOrganizationRodoModerator = ClaimsHelper.IsRoleOrganizationRodoModerator(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a team guardian or organization RODO moderator can view RODO documents");
            }

            bool isUserTeamAdmin = await _db.Teams.AsNoTracking().AnyAsync(tm => tm.Id == teamId && tm.GuardianId == userId);

            if (!isUserTeamAdmin && !isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only this team's guardian or an organization RODO moderator can view its RODO documents");
            }

            var query = _db.TeamMembers
                .Include(tm => tm.Rodo)
                .Include(tm => tm.Team).ThenInclude(team => team.Captain)
                .AsNoTracking()
                .AsQueryable();

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
                        tm.Rodo?.Version ?? null,
                        tm.Rodo?.CreatedAt ?? null,
                        tm.Rodo?.UpdatedAt ?? null,
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
            bool isOrganizationRodoModerator = ClaimsHelper.IsRoleOrganizationRodoModerator(claimsPrincipal);

            if (!isOrganizationRodoModerator)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only an organization RODO moderator can change RODO status");
            }

            var teamMember = await _db.TeamMembers.Include(tm => tm.Rodo).Include(tm => tm.Team).ThenInclude(t => t.Captain).FirstOrDefaultAsync(x => x.TeamId == teamId && x.Id == teamMemberId && x.RodoId!= null);
            if (teamMember == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberNotFound, "Specified TeamMember does not exist or does not have a rodo");
            }
            if (teamMember.Rodo.Version != rodoStateUpdateDto.RodoVersion)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }
            if (teamMember.Version != rodoStateUpdateDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }

            teamMember.RodoState = rodoStateUpdateDto.State;
            teamMember.UpdatedAt = DateTime.UtcNow;
            teamMember.Version++;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }

            return new TeamMemberDto
                    (
                        teamMember.Id,
                        teamMember.TeamId,
                        teamMember.NameFirst == null ? teamMember.Team.Captain.NameFirst! : teamMember.NameFirst!,
                        teamMember.NameLast == null ? teamMember.Team.Captain.NameLast! : teamMember.NameLast!,
                        teamMember.RodoState,
                        teamMember.Rodo?.Version ?? null,
                        teamMember.Rodo?.CreatedAt ?? null,
                        teamMember.Rodo?.UpdatedAt ?? null,
                        teamMember.CreatedAt,
                        teamMember.UpdatedAt,
                        teamMember.Version
                    );
        }
    }
}
