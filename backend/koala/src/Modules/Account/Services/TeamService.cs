using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;

namespace koala.src.Modules.Account.Services
{
    public interface ITeamService
    {
        public Task<TeamDto> CreateTeamAsync(ClaimsPrincipal? claimsPrincipal, TeamCreateRequestDto requestDto);
        public Task DeleteTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId);
        public Task<List<TeamMemberDto>> AddTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamMemberUpsertRequestDto requestDto);
        public Task<List<TeamMemberDto>> UpdateTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, TeamMemberUpsertRequestDto requestDto);
        public Task DeleteTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId);
        public Task<TeamDto> UpdateTeamNameAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamUpdateNameRequestDto requestDto);
        public Task<TeamJoinCodeDto> CreateJoinTeamCodeAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId);
        public Task<TeamDto> JoinTeamWithCodeAsync(ClaimsPrincipal? claimsPrincipal, string JoinCode);
        public Task<(List<TeamDto>, ApiPagination)> GetMyTeamsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, TeamQueryDto teamQueryDto);
        public Task<TeamDto> GetTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId);
        public Task<(List<TeamDto>, ApiPagination)> GetTeamsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, TeamQueryDto teamQueryDto, TeamMemberQueryDto teamMemberQueryDto);
    }
    public class TeamService : ITeamService
    {
        private readonly ICoreModule _coreService;
        private readonly AccountDbContext _db;

        private const string CAPTAIN = "CAPTAIN";
        private const string ADMIN = "ADMIN";
        private class TeamJoinCodeGenerator
        {
            // Excludes visually confusing characters: 0, O, 1, I, L
            private const string SafeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            private const int DefaultLength = 8;

            public static string GenerateCode(int length = DefaultLength)
            {
                var data = new byte[length];
                RandomNumberGenerator.Fill(data);

                var result = new StringBuilder(length);
                foreach (var b in data)
                {
                    // Use modulo bias correction in high-scale systems, 
                    // but for an 8-character code with a 32-char alphabet, modulo is safe enough.
                    result.Append(SafeAlphabet[b % SafeAlphabet.Length]);
                }

                return result.ToString();
            }
        }

        public TeamService(AccountDbContext db, ICoreModule coreService)
        {
            _db = db;
            _coreService = coreService;
        }

        public async Task<TeamDto> CreateTeamAsync(ClaimsPrincipal? claimsPrincipal, TeamCreateRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player can create a team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new KoalaException(KoalaErrorCodes.ActiveEditionNotFound, "There is no active edition under which the team could be created");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (timeNow < activeEdition.TeamRegisterOpen || timeNow > activeEdition.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.TeamChangesWindowClosed, "Time span for team changes no longer permited");
            }

            var school = await _coreService.Internal_GetSchoolAsync(requestDto.SchoolId);

            if (school == null)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "There is no school with provided id under which the team could be created");
            }

            var user = await _db.Users.Include(u => u.CaptainTeam).Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptainAlready = user.CaptainTeam != null ? true : false;
            if (isCaptainAlready)
            {
                throw new KoalaException(KoalaErrorCodes.UserIsCaptainOfTeamAlready, "User already is part of a team, cannot be a part of more than one team");
            }

            Team team = new Team
            {
                Id = Guid.CreateVersion7(),
                SchoolId = requestDto.SchoolId,
                EditionId = activeEdition.Id,
                CaptainId = userId,
                GuardianId = null,
                Name = requestDto.Name,
                NameAccepted = false,
                Tier = school.Tier,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            TeamMember memberTeam = new TeamMember
            {
                Id = userId,
                TeamId = team.Id,
                RodoState = TeamMemberRodoState.RODOSTATE_NONE,
                NameFirst = null,
                NameLast = null,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            await _db.Teams.AddAsync(team);
            await _db.TeamMembers.AddAsync(memberTeam);
            await _db.SaveChangesAsync();


            return new TeamDto
            (
                team.Id,
                team.EditionId,
                team.SchoolId,
                team.Name,
                team.Tier,
                !team.NameAccepted,
                team.CreatedAt,
                team.UpdatedAt,
                team.Version,
                new UserDto
                (
                    user.Id,
                    user.NameFirst!,
                    user.NameLast!,
                    user.Email,
                    user.Censored,
                    user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                    user.CreatedAt,
                    user.UpdatedAt,
                    user.Version
                ),
                null,
                new List<TeamMemberDto>(),
                null
            );
        }

        public async Task DeleteTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new KoalaException(KoalaErrorCodes.ActiveEditionNotFound, "Cannot edit or delete team when no edition is currently active");
            }

            var captain = await _db.Users.Include(u => u.CaptainTeam).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotPartOfThisTeam, "User is not a captain of this team");
            }

            var teamMembers = await _db.TeamMembers.Where(tm => tm.TeamId == teamId).ToListAsync();

            _db.Teams.Remove(captain.CaptainTeam);
            _db.TeamMembers.RemoveRange(teamMembers);

            await _db.SaveChangesAsync();

            return;
        }
        //FIXME: ADD SAFTY CHECKS FOR CONCURENCY UPDATES
        public async Task<List<TeamMemberDto>> AddTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamMemberUpsertRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (timeNow < activeEdition.TeamRegisterOpen || timeNow > activeEdition.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.TeamChangesWindowClosed, "Time span for team changes no longer permited");
            }

            var captain = await _db.Users.Include(u => u.CaptainTeam).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "User is not a captain of this team");
            }

            int membersCount = await _db.TeamMembers.Where(tm => tm.TeamId == teamId).CountAsync();

            if (membersCount >= 4)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberLimitExceeded, "Team is full");
            }

            TeamMember newTeamMember = new TeamMember
            {
                Id = Guid.CreateVersion7(),
                TeamId = teamId,
                RodoId = null,
                RodoState = TeamMemberRodoState.RODOSTATE_NONE,
                NameFirst = requestDto.NameFirst,
                NameLast = requestDto.NameLast,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            await _db.TeamMembers.AddAsync(newTeamMember);

            await _db.SaveChangesAsync();

            return await _db.TeamMembers.Where(tm => tm.TeamId == teamId && tm.Id != userId)
                .Select(tm => new TeamMemberDto
                (
                    tm.Id,
                    tm.TeamId,
                    tm.NameFirst!,
                    tm.NameLast!,
                    tm.RodoState,
                    tm.Rodo == null ? null : tm.Rodo.Version,
                    tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                    tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                    tm.CreatedAt,
                    tm.UpdatedAt,
                    tm.Version
                )).ToListAsync();
        }
        //FIXME: ADD SAFTY CHECKS FOR CONCURENCY UPDATES
        public async Task<List<TeamMemberDto>> UpdateTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, TeamMemberUpsertRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (timeNow < activeEdition.TeamRegisterOpen || timeNow > activeEdition.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.TeamChangesWindowClosed, "Time span for team changes no longer permited");
            }

            var captain = await _db.Users.Include(u => u.CaptainTeam).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "User is not a captain of this team");
            }

            var teamMemberToUpdate = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.Id != userId);

            if (teamMemberToUpdate == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberNotFound, "Could not find the member relation like this");
            }

            if (teamMemberToUpdate.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Member field version does not match make sure u have the fresh data");
            }

            teamMemberToUpdate.NameFirst = requestDto.NameFirst;
            teamMemberToUpdate.NameLast = requestDto.NameLast;
            teamMemberToUpdate.UpdatedAt = timeNow;
            teamMemberToUpdate.Version++;

            await _db.SaveChangesAsync();

            return await _db.TeamMembers.Where(tm => tm.TeamId == teamId && tm.Id != userId)
                .Select(tm => new TeamMemberDto
                (
                    tm.Id,
                    tm.TeamId,
                    tm.NameFirst!,
                    tm.NameLast!,
                    tm.RodoState,
                    tm.Rodo == null ? null : tm.Rodo.Version,
                    tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                    tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                    tm.CreatedAt,
                    tm.UpdatedAt,
                    tm.Version
                )).ToListAsync();
        }
        public async Task DeleteTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (timeNow < activeEdition.TeamRegisterOpen || timeNow > activeEdition.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.TeamChangesWindowClosed, "Time span for team changes no longer permited");
            }

            var captain = await _db.Users.Include(u => u.CaptainTeam).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "User is not a captain of this team");
            }

            var teamMemberToDelete = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Id == teamMemberId && tm.Id != userId);

            if (teamMemberToDelete == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberNotFound, "Could not find the member relation like this");
            }

            _db.TeamMembers.Remove(teamMemberToDelete);

            await _db.SaveChangesAsync();

            return;
        }
        //FIXME: ADD SAFTY CHECKS FOR CONCURENCY UPDATES TRANSACTION !!!
        public async Task<TeamDto> UpdateTeamNameAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamUpdateNameRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new KoalaException(KoalaErrorCodes.ActiveEditionNotFound, "Cannot edit team name when no edition is currently active");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (timeNow < activeEdition.TeamRegisterOpen || timeNow > activeEdition.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.TeamChangesWindowClosed, "Time span for team changes no longer permited");
            }

            var captain = await _db.Users.Include(u => u.CaptainTeam).ThenInclude(t => t.Guardian).ThenInclude(g => g.UserRoles).ThenInclude(ur => ur.Role).Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "User is not a captain of this team");
            }

            if (captain.CaptainTeam.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "team version field does not match make sure u have the fresh data");
            }

            captain.CaptainTeam.Name = requestDto.Name;
            captain.CaptainTeam.NameAccepted = false;
            captain.CaptainTeam.UpdatedAt = timeNow;
            captain.CaptainTeam.Version++;

            await _db.SaveChangesAsync();

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == teamId && tm.Id != captain.Id)
                .Select(tm => new TeamMemberDto
                (
                    tm.Id,
                    tm.TeamId,
                    tm.NameFirst!,
                    tm.NameLast!,
                    tm.RodoState,
                    tm.Rodo == null ? null : tm.Rodo.Version,
                    tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                    tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                    tm.CreatedAt,
                    tm.UpdatedAt,
                    tm.Version
                )).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.UpdatedAt, teamJoinCode.ExpiresAt);
            UserDto? teamAdminDto = null;
            UserDto teamCaptainDto = new UserDto
            (
                captain.Id,
                captain.NameFirst!,
                captain.NameLast!,
                captain.Email,
                captain.Censored,
                captain.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                captain.CreatedAt,
                captain.UpdatedAt,
                captain.Version
            );
            if (captain.CaptainTeam.GuardianId != null)
            {
                teamAdminDto = new UserDto
                (
                    captain.CaptainTeam.Guardian.Id,
                    captain.CaptainTeam.Guardian.NameFirst!,
                    captain.CaptainTeam.Guardian.NameLast!,
                    captain.CaptainTeam.Guardian.Email,
                    captain.CaptainTeam.Guardian.Censored,
                    captain.CaptainTeam.Guardian.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                    captain.CaptainTeam.Guardian.CreatedAt,
                    captain.CaptainTeam.Guardian.UpdatedAt,
                    captain.CaptainTeam.Guardian.Version
                );
            }

            return new TeamDto
            (
                captain.CaptainTeam.Id,
                captain.CaptainTeam.EditionId,
                captain.CaptainTeam.SchoolId,
                captain.CaptainTeam.Name,
                captain.CaptainTeam.Tier,
                !captain.CaptainTeam.NameAccepted,
                captain.CaptainTeam.CreatedAt,
                captain.CaptainTeam.UpdatedAt,
                captain.CaptainTeam.Version,
                teamCaptainDto,
                teamAdminDto,
                teamMembers,
                teamJoinCodeDto
            );
        }
        //FIXME: ADD SAFTY CHECKS FOR CONCURENCY UPDATES
        public async Task<TeamJoinCodeDto> CreateJoinTeamCodeAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            DateTime timeNow = DateTime.UtcNow;

            var captain = await _db.Users.Include(u => u.CaptainTeam).FirstOrDefaultAsync(u => u.Id == userId && u.Verified);
            if (captain == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "This user does not exist");
            }

            bool isCaptain = captain.CaptainTeam != null ? true : false;
            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotCaptainOfThisTeam, "User is not a captain");
            }

            if (captain.CaptainTeam.Id != teamId)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "User is not a captain of this team");
            }

            TeamJoinCode? teamJoinCode = await _db.TeamJoinCodes.FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            if (teamJoinCode == null)
            {
                teamJoinCode = new TeamJoinCode
                {
                    TeamId = teamId,
                    JoinCode = TeamJoinCodeGenerator.GenerateCode(8),
                    CreatedAt = timeNow,
                    ExpiresAt = timeNow.AddDays(1)
                };

                await _db.TeamJoinCodes.AddAsync(teamJoinCode);
            }
            else
            {
                teamJoinCode.JoinCode = TeamJoinCodeGenerator.GenerateCode(8);
                teamJoinCode.CreatedAt = timeNow;
                teamJoinCode.ExpiresAt = timeNow.AddDays(1);
            }

            await _db.SaveChangesAsync();

            return new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.UpdatedAt, teamJoinCode.ExpiresAt);
        }
        //FIXME: ADD SAFTY CHECKS FOR CONCURENCY UPDATES TRANSACTION!!!
        public async Task<TeamDto> JoinTeamWithCodeAsync(ClaimsPrincipal? claimsPrincipal, string JoinCode)
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
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team admin can use the join code");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot join to team when no edition is currently active");
            }

            var teamJoinCode = await _db.TeamJoinCodes
                .FirstOrDefaultAsync(tjc => tjc.JoinCode == JoinCode);

            if (teamJoinCode == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamJoinCodeNotFound, "The join code does not exist or is expired");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (teamJoinCode.ExpiresAt <= timeNow)
            {
                _db.TeamJoinCodes.Remove(teamJoinCode);
                await _db.SaveChangesAsync();

                throw new KoalaException(KoalaErrorCodes.TeamJoinCodeNotFound, "The join code is invalid or has expired.");
            }

            var team = await _db.Teams.Include(t => t.Captain).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(t => t.Guardian).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.Id == teamJoinCode.TeamId);

            if (team == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "team for this join code does not exist");
            }

            if (team.EditionId != activeEdition.Id)
            {
                throw new KoalaException(KoalaErrorCodes.TeamJoinCodeNotFound, "The join code belongs to an inactive edition");
            }

            // MAKE SURE ONLY ONE TEAM ADMIN PER TEAM AND WELL TEAM PLAYER CANT JOIN SINCE A TEAM WITH NO CAPTAIN CAN EXIST AND ONLY ONE CAPTAIN PER TEAM IS ALLOWED
            bool teamHasAdmin = team.GuardianId != null ? true : false;

            if (teamHasAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.TeamMemberLimitExceeded, "This team already has an admin");
            }

            team.GuardianId = userId;

            await _db.SaveChangesAsync();
            await _db.Entry(team).Reference(t => t.Guardian).LoadAsync();

            var teamMembers = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamJoinCode.TeamId && tm.Id != team.CaptainId)
                .Select(tm => new TeamMemberDto
                (
                    tm.Id,
                    tm.TeamId,
                    tm.NameFirst!,
                    tm.NameLast!,
                    tm.RodoState,
                    tm.Rodo == null ? null : tm.Rodo.Version,
                    tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                    tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                    tm.CreatedAt,
                    tm.UpdatedAt,
                    tm.Version
                ))
                .ToListAsync();

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.UpdatedAt, teamJoinCode.ExpiresAt);

            UserDto teamCaptainDto = new UserDto
            (
                team.Captain.Id,
                team.Captain.NameFirst!,
                team.Captain.NameLast!,
                team.Captain.Email,
                team.Captain.Censored,
                team.Captain.UserRoles?.Select(ur => ur.Role.Name).Distinct().ToList() ?? new List<string>(),
                team.Captain.CreatedAt,
                team.Captain.UpdatedAt,
                team.Captain.Version
            );

            UserDto teamGuardianDto = new UserDto
            (
                team.Guardian.Id,
                team.Guardian.NameFirst!,
                team.Guardian.NameLast!,
                team.Guardian.Email,
                team.Guardian.Censored,
                team.Guardian.UserRoles?.Select(ur => ur.Role.Name).Distinct().ToList() ?? new List<string>(),
                team.Guardian.CreatedAt,
                team.Guardian.UpdatedAt,
                team.Guardian.Version
            );

            return new TeamDto
            (
                team.Id,
                team.EditionId,
                team.SchoolId,
                team.Name,
                team.Tier,
                !team.NameAccepted,
                team.CreatedAt,
                team.UpdatedAt,
                team.Version,
                teamCaptainDto,
                teamGuardianDto,
                teamMembers,
                teamJoinCodeDto
            );
        }

        public async Task<(List<TeamDto>, ApiPagination)> GetMyTeamsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, TeamQueryDto teamQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer && !isTeamAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team members can view their teams");
            }

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Corrupted auth cookie");
            }

            var query = _db.Teams
                .AsNoTracking()
                .Where(t => t.CaptainId == userId || t.GuardianId == userId);

            if (teamQueryDto.EditionId != null)
            {
                query = query.Where(t => t.EditionId == teamQueryDto.EditionId);
            }

            if (!string.IsNullOrEmpty(teamQueryDto.Name))
            {
                query = query.Where(t => t.Name == teamQueryDto.Name);
            }

            int totalCount = await query.CountAsync();

            var teamDtos = await query
                .OrderBy(t => t.CreatedAt)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(qr => new TeamDto(
                    qr.Id,
                    qr.EditionId,
                    qr.SchoolId,
                    qr.Name,
                    qr.Tier,
                    !qr.NameAccepted,
                    qr.CreatedAt,
                    qr.UpdatedAt,
                    qr.Version,

                    new UserDto
                    (
                        qr.Captain.Id,
                        qr.Captain.NameFirst!,
                        qr.Captain.NameLast!,
                        qr.Captain.Email,
                        qr.Captain.Censored,
                        qr.Captain.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                        qr.Captain.CreatedAt,
                        qr.Captain.UpdatedAt,
                        qr.Captain.Version
                    ),
                    qr.Guardian == null ? null : new UserDto
                    (
                        qr.Guardian.Id,
                        qr.Guardian.NameFirst!,
                        qr.Guardian.NameLast!,
                        qr.Guardian.Email,
                        qr.Guardian.Censored,
                        qr.Guardian.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                        qr.Guardian.CreatedAt,
                        qr.Guardian.UpdatedAt,
                        qr.Guardian.Version
                    ),

                    qr.TeamMembers
                        .Where(tm => tm.Id != qr.CaptainId)
                        .Select(tm => new TeamMemberDto
                        (
                            tm.Id,
                            tm.TeamId,
                            tm.NameFirst!,
                            tm.NameLast!,
                            tm.RodoState,
                            tm.Rodo == null ? null : tm.Rodo.Version,
                            tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                            tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                            tm.CreatedAt,
                            tm.UpdatedAt,
                            tm.Version
                        ))
                        .ToList(),

                    qr.TeamJoinCode == null || qr.TeamJoinCode.ExpiresAt <= DateTime.UtcNow
                        ? null
                        : new TeamJoinCodeDto(qr.TeamJoinCode.JoinCode, qr.TeamJoinCode.CreatedAt, qr.TeamJoinCode.UpdatedAt, qr.TeamJoinCode.ExpiresAt)
                ))
                .ToListAsync();

            return (teamDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));

        }
        public async Task<TeamDto> GetTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsRoleGuardian(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer && !isTeamAdmin && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team members and admin can view the team");
            }

            bool isTeamMember = await _db.Teams.AnyAsync(tm => tm.Id == teamId && (tm.CaptainId == userId || tm.GuardianId == userId));

            if (!isTeamMember && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only team members and admin can view the team");
            }

            var team = await _db.Teams.AsNoTracking().Include(t => t.Captain).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).Include(t => t.Guardian).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new KoalaException(KoalaErrorCodes.TeamNotFound, "The provided id does not belong to any team");
            }

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == teamId && tm.Id != team.CaptainId)
                .Select(tm => new TeamMemberDto
                (
                    tm.Id,
                    tm.TeamId,
                    tm.NameFirst!,
                    tm.NameLast!,
                    tm.RodoState,
                    tm.Rodo == null ? null : tm.Rodo.Version,
                    tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                    tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                    tm.CreatedAt,
                    tm.UpdatedAt,
                    tm.Version
                )).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null || teamJoinCode.ExpiresAt <= DateTime.UtcNow
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.UpdatedAt, teamJoinCode.ExpiresAt);

            UserDto teamCaptainDto = new UserDto
            (
                team.Captain.Id,
                team.Captain.NameFirst!,
                team.Captain.NameLast!,
                team.Captain.Email,
                team.Captain.Censored,
                team.Captain.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                team.Captain.CreatedAt,
                team.Captain.UpdatedAt,
                team.Captain.Version
            );
            UserDto? teamAdminDto = team.Guardian == null ? null : new UserDto
            (
                team.Guardian.Id,
                team.Guardian.NameFirst!,
                team.Guardian.NameLast!,
                team.Guardian.Email,
                team.Guardian.Censored,
                team.Guardian.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                team.Guardian.CreatedAt,
                team.Guardian.UpdatedAt,
                team.Guardian.Version
            );

            return new TeamDto
            (
                team.Id,
                team.EditionId,
                team.SchoolId,
                team.Name,
                team.Tier,
                !team.NameAccepted,
                team.CreatedAt,
                team.UpdatedAt,
                team.Version,
                teamCaptainDto,
                teamAdminDto,
                teamMembers,
                teamJoinCodeDto
            );
        }
        public async Task<(List<TeamDto>, ApiPagination)> GetTeamsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, TeamQueryDto teamQueryDto, TeamMemberQueryDto teamMemberQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only admin can view all the teams");
            }

            var query = _db.Teams
                .AsNoTracking()
                .Include(t => t.TeamMembers)
                .Include(t => t.TeamJoinCode)
                .AsQueryable();

            if (teamQueryDto.EditionId != null)
            {
                query = query.Where(t => t.EditionId == teamQueryDto.EditionId);
            }

            if (teamQueryDto.CaptainId != null)
            {
                query = query.Where(t => t.CaptainId == teamQueryDto.CaptainId);
            }

            if (teamQueryDto.GuardianId != null)
            {
                query = query.Where(t => t.GuardianId == teamQueryDto.GuardianId);
            }

            if (teamQueryDto.SchoolId != null)
            {
                query = query.Where(t => t.SchoolId == teamQueryDto.SchoolId);
            }

            if (!string.IsNullOrEmpty(teamQueryDto.Name))
            {
                query = query.Where(t => t.Name == teamQueryDto.Name);
            }

            if (teamMemberQueryDto.MemberId != null)
            {
                query = query.Where(t => t.TeamMembers.Any(tm => tm.Id == teamMemberQueryDto.MemberId && tm.Id != tm.Team.CaptainId));
            }

            var queryResults = await query.ToListAsync();
            var teamDtos = await query
                .OrderBy(t => t.CreatedAt)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(qr => new TeamDto(
                    qr.Id,
                    qr.EditionId,
                    qr.SchoolId,
                    qr.Name,
                    qr.Tier,
                    !qr.NameAccepted,
                    qr.CreatedAt,
                    qr.UpdatedAt,
                    qr.Version,

                    new UserDto
                    (
                        qr.Captain.Id,
                        qr.Captain.NameFirst!,
                        qr.Captain.NameLast!,
                        qr.Captain.Email,
                        qr.Captain.Censored,
                        qr.Captain.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                        qr.Captain.CreatedAt,
                        qr.Captain.UpdatedAt,
                        qr.Captain.Version
                    ),
                    qr.Guardian == null ? null : new UserDto
                    (
                        qr.Guardian.Id,
                        qr.Guardian.NameFirst!,
                        qr.Guardian.NameLast!,
                        qr.Guardian.Email,
                        qr.Guardian.Censored,
                        qr.Guardian.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                        qr.Guardian.CreatedAt,
                        qr.Guardian.UpdatedAt,
                        qr.Guardian.Version
                    ),

                    qr.TeamMembers
                        .Where(tm => tm.Id != qr.CaptainId)
                        .Select(tm => new TeamMemberDto
                        (
                            tm.Id,
                            tm.TeamId,
                            tm.NameFirst!,
                            tm.NameLast!,
                            tm.RodoState,
                            tm.Rodo == null ? null : tm.Rodo.Version,
                            tm.Rodo == null ? null : tm.Rodo.CreatedAt,
                            tm.Rodo == null ? null : tm.Rodo.UpdatedAt,
                            tm.CreatedAt,
                            tm.UpdatedAt,
                            tm.Version
                        ))
                        .ToList(),

                    qr.TeamJoinCode == null || qr.TeamJoinCode.ExpiresAt <= DateTime.UtcNow
                        ? null
                        : new TeamJoinCodeDto(qr.TeamJoinCode.JoinCode, qr.TeamJoinCode.CreatedAt, qr.TeamJoinCode.UpdatedAt, qr.TeamJoinCode.ExpiresAt)
                ))
                .ToListAsync();

            return (teamDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }

    }
}
