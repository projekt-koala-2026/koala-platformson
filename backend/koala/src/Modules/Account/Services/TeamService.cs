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
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player can create a team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_ActiveEditionNotFound, "There is no active edition under which the team could be created");
            }

            var schoolExist = await _coreService.Internal_ExistSchoolAsync(requestDto.SchoolId);

            if (schoolExist == false)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_SchoolNotFound, "There is no school with provided id under which the team could be created");
            }

            bool isCaptainAlready = await _db.UserTeams.AsNoTracking().AnyAsync(ut => ut.UserId == userId && ut.Position == "PLAYER");
            if (isCaptainAlready)
            {
                throw new AccountException(AccountErrorCodes.UserIsAPartOfTeamAlready, "User already is part of a team, cannot be a part of more than one team");
            }

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.Verified == true);

            if (user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound, "This user does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            Team team = new Team
            {
                Id = Guid.CreateVersion7(),
                SchoolId = requestDto.SchoolId,
                EditionId = activeEdition.Id,
                Name = requestDto.Name,
                NameAccepted = false,
                CreatedAt = timeNow
            };

            UserTeam userTeam = new UserTeam
            {
                TeamId = team.Id,
                UserId = userId,
                Position = CAPTAIN
            };

            TeamMember memberTeam = new TeamMember
            {
                Id = Guid.CreateVersion7(),
                TeamId = team.Id,
                MemberId = null
            };

            await _db.Teams.AddAsync(team);
            await _db.UserTeams.AddAsync(userTeam);
            await _db.TeamMembers.AddAsync(memberTeam);
            await _db.SaveChangesAsync();


            return new TeamDto
            (
                team.Id,
                team.EditionId,
                team.SchoolId,
                team.Name,
                !team.NameAccepted,
                team.CreatedAt,
                new UserDto
                (
                    user.Id,
                    user.NameFirst!,
                    user.NameLast!,
                    user.Email,
                    user.Censored,
                    user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
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
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_ActiveEditionNotFound, "Cannot edit or delete team when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId && ut.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            var teamMembers = await _db.TeamMembers.Where(tm => tm.TeamId == teamId).ToListAsync();

            _db.Teams.Remove(teamCaptain.Team);
            _db.TeamMembers.RemoveRange(teamMembers);

            await _db.SaveChangesAsync();

            return;
        }
        public async Task<List<TeamMemberDto>> AddTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamMemberUpsertRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId && ut.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            int membersCount = await _db.TeamMembers.Where(tm => tm.TeamId == teamId).CountAsync();

            if (membersCount >= 3)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberCountMax, "Team is full");
            }

            Member newMember = new Member
            {
                Id = Guid.CreateVersion7(),
                NameFirst = requestDto.NameFirst,
                NameLast = requestDto.NameLast
            };

            TeamMember newTeamMember = new TeamMember
            {
                Id = Guid.CreateVersion7(),
                TeamId = teamId,
                MemberId = newMember.Id
            };

            await _db.Members.AddAsync(newMember);
            await _db.TeamMembers.AddAsync(newTeamMember);

            await _db.SaveChangesAsync();

            return await _db.TeamMembers.Where(tm => tm.TeamId == teamId && tm.MemberId != null).Select(tm => new TeamMemberDto(tm.MemberId, tm.Member.NameFirst, tm.Member.NameLast)).ToListAsync();
        }
        public async Task<List<TeamMemberDto>> UpdateTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId, TeamMemberUpsertRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId && ut.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            var teamMemberToUpdate = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.MemberId == teamMemberId);
            var memberToUpdate = await _db.Members.FirstOrDefaultAsync(m => m.Id == teamMemberId);

            if (teamMemberToUpdate == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "Could not find the member relation like this");
            }

            if (memberToUpdate == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "Could not find the member");
            }

            memberToUpdate.NameFirst = requestDto.NameFirst;
            memberToUpdate.NameLast = requestDto.NameLast;

            await _db.SaveChangesAsync();

            return await _db.TeamMembers.Where(tm => tm.TeamId == teamId && tm.MemberId != null).Select(tm => new TeamMemberDto(tm.MemberId, tm.Member.NameFirst, tm.Member.NameLast)).ToListAsync();
        }
        public async Task DeleteTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId && ut.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            var teamMemberToDelete = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.MemberId == teamMemberId);
            var memberToDelete = await _db.Members.FirstOrDefaultAsync(m => m.Id == teamMemberId);

            if (teamMemberToDelete == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "Could not find the member relation like this");
            }

            if (memberToDelete == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "Could not find the member");
            }

            _db.TeamMembers.Remove(teamMemberToDelete);
            _db.Members.Remove(memberToDelete);

            await _db.SaveChangesAsync();

            return;
        }

        public async Task<TeamDto> UpdateTeamNameAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, TeamUpdateNameRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_ActiveEditionNotFound, "Cannot edit team name when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId && tm.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            teamCaptain.Team.Name = requestDto.Name;
            teamCaptain.Team.NameAccepted = false;

            await _db.SaveChangesAsync();

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == teamId && tm.MemberId != null).Select(tm => new TeamMemberDto(tm.MemberId, tm.Member.NameFirst, tm.Member.NameLast)).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);

            var teamAdmin = await _db.UserTeams.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.Position == ADMIN);
            UserDto? teamAdminDto = null;
            UserDto teamCaptainDto = new UserDto
            (
                teamCaptain.User.Id,
                teamCaptain.User.NameFirst!,
                teamCaptain.User.NameLast!,
                teamCaptain.User.Email,
                teamCaptain.User.Censored,
                teamCaptain.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
            );
            if (teamAdmin != null)
            {
                teamAdminDto = new UserDto
                (
                    teamAdmin.User.Id,
                    teamAdmin.User.NameFirst!,
                    teamAdmin.User.NameLast!,
                    teamAdmin.User.Email,
                    teamAdmin.User.Censored,
                    teamAdmin.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                );
            }

            return new TeamDto
            (
                teamCaptain.Team.Id,
                teamCaptain.Team.EditionId,
                teamCaptain.Team.SchoolId,
                teamCaptain.Team.Name,
                teamCaptain.Team.NameAccepted,
                teamCaptain.Team.CreatedAt,
                teamCaptainDto,
                teamAdminDto,
                teamMembers,
                teamJoinCodeDto
            );
        }

        public async Task<TeamJoinCodeDto> CreateJoinTeamCodeAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot create join codes for team when no edition is currently active");
            }

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId && tm.Position == CAPTAIN);
            if (teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound, "User is not a captain of the team");
            }

            TeamJoinCode? teamJoinCode = await _db.TeamJoinCodes.FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            DateTime timeNow = DateTime.UtcNow;

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

            return new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);
        }

        public async Task<TeamDto> JoinTeamWithCodeAsync(ClaimsPrincipal? claimsPrincipal, string JoinCode)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team admin can use the join code");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEditionAsync();

            if (activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot join to team when no edition is currently active");
            }

            var teamJoinCode = await _db.TeamJoinCodes
                .Include(tjc => tjc.Team)
                .FirstOrDefaultAsync(tjc => tjc.JoinCode == JoinCode);

            if (teamJoinCode == null)
            {
                throw new AccountException(AccountErrorCodes.TeamJoinCodeNotFound, "The join code does not exist or is expired");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (teamJoinCode.ExpiresAt <= timeNow)
            {
                _db.TeamJoinCodes.Remove(teamJoinCode);
                await _db.SaveChangesAsync();

                throw new AccountException(
                    AccountErrorCodes.TeamJoinCodeNotFound,
                    "The join code is invalid or has expired."
                );
            }

            if (teamJoinCode.Team.EditionId != activeEdition.Id)
            {
                throw new AccountException(AccountErrorCodes.TeamJoinCodeNotFound, "The join code belongs to an inactive edition");
            }

            // MAKE SURE ONLY ONE TEAM ADMIN PER TEAM AND WELL TEAM PLAYER CANT JOIN SINCE A TEAM WITH NO CAPTAIN CAN EXIST AND ONLY ONE CAPTAIN PER TEAM IS ALLOWED
            var teamHasAdmin = await _db.UserTeams.FirstOrDefaultAsync(ut => ut.TeamId == teamJoinCode.TeamId && ut.Position == ADMIN);

            if (teamHasAdmin != null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberCountMax, "This team already has an admin");
            }

            UserTeam newTeamAdmin = new UserTeam
            {
                UserId = userId,
                TeamId = teamJoinCode.TeamId,
                Position = ADMIN
            };

            await _db.UserTeams.AddAsync(newTeamAdmin);
            await _db.SaveChangesAsync();


            var adminUser = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == userId);

            var teamMembers = await _db.TeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamJoinCode.TeamId && tm.MemberId != null)
                .Select(tm => new TeamMemberDto(tm.MemberId, tm.Member.NameFirst, tm.Member.NameLast))
                .ToListAsync();

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);

            var teamCaptain = await _db.UserTeams
                .Include(tm => tm.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(tm => tm.TeamId == teamJoinCode.TeamId && tm.Position == CAPTAIN);

            UserDto teamCaptainDto = new UserDto
            (
                teamCaptain.User.Id,
                teamCaptain.User.NameFirst!,
                teamCaptain.User.NameLast!,
                teamCaptain.User.Email,
                teamCaptain.User.Censored,
                teamCaptain.User.UserRoles?.Select(ur => ur.Role.Name).Distinct().ToList() ?? new List<string>()
            );

            UserDto teamAdminDto = new UserDto
            (
                adminUser.Id,
                adminUser.NameFirst!,
                adminUser.NameLast!,
                adminUser.Email,
                adminUser.Censored,
                adminUser.UserRoles?.Select(ur => ur.Role.Name).Distinct().ToList() ?? new List<string>()
            );

            var team = teamJoinCode.Team;

            return new TeamDto
            (
                team.Id,
                team.EditionId,
                team.SchoolId,
                team.Name,
                team.NameAccepted,
                team.CreatedAt,
                teamCaptainDto,
                teamAdminDto,
                teamMembers,
                teamJoinCodeDto
            );
        }

        public async Task<(List<TeamDto>, ApiPagination)> GetMyTeamsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, TeamQueryDto teamQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer && !isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team members can view their teams");
            }

            if (userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Corrupted auth cookie");
            }

            var query = _db.Teams
                .AsNoTracking()
                .Where(t => t.UserTeams.Any(tm => tm.UserId == userId));

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
                    !qr.NameAccepted,
                    qr.CreatedAt,

                    qr.UserTeams
                        .Where(ut => ut.Position == CAPTAIN)
                        .Select(ut => new UserDto(
                            ut.User.Id,
                            ut.User.NameFirst!,
                            ut.User.NameLast!,
                            ut.User.Email,
                            ut.User.Censored,
                            ut.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                        ))
                        .FirstOrDefault()!,

                    qr.UserTeams
                        .Where(ut => ut.Position == ADMIN)
                        .Select(ut => new UserDto(
                            ut.User.Id,
                            ut.User.NameFirst!,
                            ut.User.NameLast!,
                            ut.User.Email,
                            ut.User.Censored,
                            ut.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                        ))
                        .FirstOrDefault(),

                    qr.TeamMembers
                        .Where(tm => tm.MemberId != null)
                        .Select(tm => new TeamMemberDto(tm.Member.Id, tm.Member.NameFirst, tm.Member.NameLast))
                        .ToList(),

                    qr.TeamJoinCode == null || qr.TeamJoinCode.ExpiresAt <= DateTime.UtcNow
                        ? null
                        : new TeamJoinCodeDto(qr.TeamJoinCode.JoinCode, qr.TeamJoinCode.CreatedAt, qr.TeamJoinCode.ExpiresAt)
                ))
                .ToListAsync();

            return (teamDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));

        }

        //TODO fix this just go through this and make sure its corect this and the next one XDD
        public async Task<TeamDto> GetTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isTeamPlayer && !isTeamAdmin && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team members and admin can view the team");
            }

            bool isTeamMember = await _db.UserTeams.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);

            if (!isTeamMember && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only team members and admin can view the team");
            }

            var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new AccountException(AccountErrorCodes.TeamNotFound, "The provided id does not belong to any team");
            }

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm => tm.TeamId == teamId && tm.MemberId != null).Select(tm => new TeamMemberDto(tm.Member.Id, tm.Member.NameFirst, tm.Member.NameLast)).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            TeamJoinCodeDto? teamJoinCodeDto = teamJoinCode == null || teamJoinCode.ExpiresAt <= DateTime.UtcNow
                ? null
                : new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);

            var teamCaptain = await _db.UserTeams.FirstOrDefaultAsync(tm => tm.TeamId == teamJoinCode.TeamId && tm.Position == CAPTAIN);
            var teamAdmin = await _db.UserTeams.FirstOrDefaultAsync(tm => tm.TeamId == teamJoinCode.TeamId && tm.Position == ADMIN);

            UserDto teamCaptainDto = new UserDto
            (
                teamCaptain.User.Id,
                teamCaptain.User.NameFirst!,
                teamCaptain.User.NameLast!,
                teamCaptain.User.Email,
                teamCaptain.User.Censored,
                teamCaptain.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
            );
            UserDto teamAdminDto = new UserDto
            (
                teamAdmin.User.Id,
                teamAdmin.User.NameFirst!,
                teamAdmin.User.NameLast!,
                teamAdmin.User.Email,
                teamAdmin.User.Censored,
                teamAdmin.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
            );

            return new TeamDto
            (
                teamCaptain.Team.Id,
                teamCaptain.Team.EditionId,
                teamCaptain.Team.SchoolId,
                teamCaptain.Team.Name,
                teamCaptain.Team.NameAccepted,
                teamCaptain.Team.CreatedAt,
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
                throw new AccountException(AccountErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbidden, "Only admin can view all the teams");
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
                query = query.Where(t => t.UserTeams.Any(ut => ut.UserId == teamQueryDto.CaptainId && ut.Position == CAPTAIN));
            }

            if (teamQueryDto.AdminId != null)
            {
                query = query.Where(t => t.UserTeams.Any(ut => ut.UserId == teamQueryDto.AdminId && ut.Position == ADMIN));
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
                query = query.Where(t => t.TeamMembers.Any(tm => tm.MemberId == teamMemberQueryDto.MemberId));
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
                    !qr.NameAccepted,
                    qr.CreatedAt,

                    qr.UserTeams
                        .Where(ut => ut.Position == CAPTAIN)
                        .Select(ut => new UserDto(
                            ut.User.Id,
                            ut.User.NameFirst!,
                            ut.User.NameLast!,
                            ut.User.Email,
                            ut.User.Censored,
                            ut.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                        ))
                        .FirstOrDefault()!,

                    qr.UserTeams
                        .Where(ut => ut.Position == ADMIN)
                        .Select(ut => new UserDto(
                            ut.User.Id,
                            ut.User.NameFirst!,
                            ut.User.NameLast!,
                            ut.User.Email,
                            ut.User.Censored,
                            ut.User.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                        ))
                        .FirstOrDefault(),

                    qr.TeamMembers
                        .Where(tm => tm.MemberId != null)
                        .Select(tm => new TeamMemberDto(tm.Member.Id, tm.Member.NameFirst, tm.Member.NameLast))
                        .ToList(),

                    qr.TeamJoinCode == null || qr.TeamJoinCode.ExpiresAt <= DateTime.UtcNow
                        ? null
                        : new TeamJoinCodeDto(qr.TeamJoinCode.JoinCode, qr.TeamJoinCode.CreatedAt, qr.TeamJoinCode.ExpiresAt)
                ))
                .ToListAsync();

            return (teamDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }

    }
}
