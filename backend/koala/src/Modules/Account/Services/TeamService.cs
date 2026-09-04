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
    public class TeamService
    {
        private readonly ICoreModule _coreService;
        private readonly AccountDbContext _db;

        private const string CAPTAIN = "CAPTAIN";
        private const string ADMIN = "ADMIN";
        private const string PLAYER = "PLAYER";
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

        public async Task<TeamDto> CreateTeamAsync(ClaimsPrincipal? claimsPrincipal, CreateTeamRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player can create a team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new NoActiveEditionException("There is no active edition under witch the team could be created");
            }

            bool isTeamMember = await _db.TeamMembers.AsNoTracking().AnyAsync(tm=> tm.UserId == userId);
            if(isTeamMember)
            {
                throw new AccountException(AccountErrorCodes.UserIsAPartOfTeamAlready,"User already is part of a team, canot be a part of morre than one team");
            }

            DateTime timeNow = DateTime.UtcNow;

            Team team = new Team
            {
                Id = Guid.CreateVersion7(),
                Name = requestDto.Name,
                NameAccepted = false,
                CreatedAt = timeNow
            };

            TeamMember teamMember = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Position = CAPTAIN
            };

            List<TeamMemberDto> teamMemberDtos = new List<TeamMemberDto>
            {
                new TeamMemberDto(teamMember.UserId, teamMember.Position)
            };

            await _db.Teams.AddAsync(team);
            await _db.TeamMembers.AddAsync(teamMember);

            await _db.SaveChangesAsync();

            
            return new TeamDto(team.Id, team.Name, !team.NameAccepted, team.CreatedAt, teamMemberDtos, null);
        }

        public async Task DeleteTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_ActiveEditionNotFound,"Cannot edit or delete team when no edition is curently active");
            }

            var teamCaptain = await _db.TeamMembers.Include(tm => tm.Team).FirstOrDefaultAsync(tm=> tm.UserId == userId && tm.TeamId == teamId);
            if(teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound,"User is not a captain of the team");
            }

            var teamMembers = await _db.TeamMembers.Where(tm => tm.TeamId == teamId).ToListAsync();

            _db.Teams.Remove(teamCaptain.Team);
            _db.TeamMembers.RemoveRange(teamMembers);

            await _db.SaveChangesAsync();

            return;
        }

        public async Task DeleteTeamMemberAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, Guid teamMemberId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot edit or delete team when no edition is curently active");
            }

            var teamCaptain = await _db.TeamMembers.AsNoTracking().FirstOrDefaultAsync(tm=> tm.UserId == userId && tm.TeamId == teamId);
            if(teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound,"User is not a captain of the team");
            }

            var teamMemberToDelete = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == teamMemberId);

            if(teamMemberToDelete == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound,"Could not find the user to delete");
            }

            _db.TeamMembers.Remove(teamMemberToDelete);

            await _db.SaveChangesAsync();

            return;
        }

        public async Task<TeamDto> UpdateTeamNameAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId, UpdateTeamNameRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new AccountException(AccountErrorCodes._EXTERNAL_ActiveEditionNotFound,"Cannot edit team name when no edition is curently active");
            }

            var teamCaptain = await _db.TeamMembers.Include(tm=> tm.Team).FirstOrDefaultAsync(tm=> tm.UserId == userId && tm.TeamId == teamId);
            if(teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound,"User is not a captain of the team");
            }
            
            teamCaptain.Team.Name = requestDto.Name;

            await _db.SaveChangesAsync();

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm=> tm.TeamId == teamId).Select(tm => new TeamMemberDto(tm.UserId, tm.Position)).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc=> tjc.TeamId == teamId);

            TeamJoinCodeDto teamJoinCodeDto = new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);

            return new TeamDto(teamCaptain.Team.Id, teamCaptain.Team.Name, !teamCaptain.Team.NameAccepted, teamCaptain.Team.CreatedAt, teamMembers, teamJoinCodeDto);
        }

        public async Task<TeamJoinCodeDto> CreateJoinTeamCodeAsync(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player with position captain can manage the team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot create join codes for team when no edition is curently active");
            }

            var teamCaptain = await _db.TeamMembers.AsNoTracking().FirstOrDefaultAsync(tm=> tm.UserId == userId && tm.TeamId == teamId);
            if(teamCaptain == null)
            {
                throw new AccountException(AccountErrorCodes.TeamMemberNotFound,"User is not a captain of the team");
            }
            
            TeamJoinCode? teamJoinCode = await _db.TeamJoinCodes.FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            DateTime timeNow = DateTime.UtcNow;

            if(teamJoinCode == null)
            {
                teamJoinCode = new TeamJoinCode
                {
                    TeamId = teamId,
                    JoinCode = TeamJoinCodeGenerator.GenerateCode(8),
                    CreatedAt = timeNow,
                    ExpiresAt = timeNow.AddHours(2.0f)
                };

                await _db.TeamJoinCodes.AddAsync(teamJoinCode);
            }
            else
            {
                teamJoinCode.JoinCode = TeamJoinCodeGenerator.GenerateCode(8);
                teamJoinCode.CreatedAt = timeNow;
                teamJoinCode.ExpiresAt = timeNow.AddMinutes(30.0f);
            }


            await _db.SaveChangesAsync();

            return new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt);
        }

        public async Task<List<TeamMemberDto>> JoinTeamWithCodeAsync(ClaimsPrincipal? claimsPrincipal, string JoinCode)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer && !isTeamAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team player and admin can join a team");
            }

            var activeEdition = await _coreService.Internal_GetTheActiveEdition();

            if(activeEdition == null)
            {
                throw new NoActiveEditionException("Cannot join to team when no edition is curently active");
            }

            var teamJoinCode = await _db.TeamJoinCodes.FirstOrDefaultAsync(tjc => tjc.JoinCode == JoinCode);

            if(teamJoinCode == null)
            {
                throw new AccountException(AccountErrorCodes.TeamJoinCodeNotFound,"The join code does not exist or is expired");
            }

            DateTime timeNow = DateTime.UtcNow;

            if(teamJoinCode.ExpiresAt >= timeNow)
            {
                _db.TeamJoinCodes.Remove(teamJoinCode);
                await _db.SaveChangesAsync();
                throw new AccountException(AccountErrorCodes.TeamJoinCodeNotFound,"The join code does not exist or is expired");
            }

            // MAKE SURE THE USER CAN ONLY JOIN ONE TEAM AS A PLAYER AND MANY AS A ADMIN (BOTH TEAM_*)

            // CHECK IF THE TEAM ALREADY HAS AN ADMIN
            // CHECK IF THE TEAM PLAYER + CAPTAIN COUNT IS 4 OR LESS
            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm=> tm.TeamId == teamJoinCode.TeamId).ToListAsync();

            if(teamMembers == null)
            {
                // WHAT THEN?
                throw new Exception();
            }

            if(teamMembers.Any(tm=> tm.UserId == userId))
            {
                throw new AccountException(AccountErrorCodes.UserIsAPartOfTeamAlready,"User already has a team exception");
            }

            if(isTeamAdmin && teamMembers.Any(t=> t.Position == ADMIN))
            {
                throw new AccountException(AccountErrorCodes.TeamMemberAlreadyExists,"This team already has a team admin in it");
            }

            if(isTeamPlayer && 4 < teamMembers.Count(t=> t.Position == PLAYER || t.Position == CAPTAIN))
            {
                throw new AccountException(AccountErrorCodes.TeamMemberCountMax,"This team already has max emount of players in it");
            }

            TeamMember teamMember = new TeamMember
            {
                TeamId = teamJoinCode.TeamId,
                UserId = userId,
                Position = ""    
            };

            if(isTeamAdmin)
            {
                teamMember.Position = ADMIN;  
            }
            if(isTeamPlayer)
            {
                teamMember.Position = PLAYER;
            }

            await _db.TeamMembers.AddAsync(teamMember);
            await _db.SaveChangesAsync();
            
            var response = teamMembers.Select(tm => new TeamMemberDto(tm.UserId, tm.Position)).ToList();
            response.Add(new TeamMemberDto(teamMember.UserId, teamMember.Position));
            return response;
        }

        public async Task<TeamDto> GetTeam(ClaimsPrincipal? claimsPrincipal, Guid teamId)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User is not loged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

            if(!isTeamPlayer && !isTeamAdmin && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team members and admin can view the team");
            }

            bool isTeamMember = await _db.TeamMembers.AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

            if(!isTeamMember && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Only team members and admin can view the team");
            }

            var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId);
        
            if(team == null)
            {
                throw new AccountException(AccountErrorCodes.TeamNotFound,"The provided id does not belong to any team");
            }

            var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm=> tm.TeamId == teamId).ToListAsync();
            var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

            TeamJoinCodeDto? teamJoinCodeDto = new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt); 

            if(teamJoinCode.ExpiresAt <= DateTime.UtcNow)
            {
                teamJoinCodeDto = null;
            }

            return new TeamDto
            (
                team.Id,team.Name,!team.NameAccepted,team.CreatedAt,
                teamMembers.Select(tm => new TeamMemberDto(tm.UserId, tm.Position)).ToList(),
                teamJoinCodeDto
            );
        }

        //FIXME: DECIDE HOW TO STRUCTURE THOSE ENDPOINTS
        // public async Task<TeamDto> GetTeams(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        // {
        //     bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

        //     if(!isAuthenticated)
        //     {
        //         throw new UnauthorizedException("User is not loged in");
        //     }
        //     bool isTeamAdmin = ClaimsHelper.IsTeamAdmin(claimsPrincipal);
        //     bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
        //     Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);

        //     if(!isTeamAdmin && !isOrganizationAdmin)
        //     {
        //         throw new ForbidenException("Only team members and admin can view the team");
        //     }

        //     bool isTeamMember = await _db.TeamMembers.AnyAsync(tm=> tm.TeamId == teamId && tm.UserId == userId);

        //     if(!isTeamMember && !isOrganizationAdmin)
        //     {
        //         throw new ForbidenException("Only team members and admin can view the team");
        //     }

        //     var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId);
        
        //     if(team == null)
        //     {
        //         throw new TeamNotFoundException("The provided id does not belong to any team");
        //     }

        //     var teamMembers = await _db.TeamMembers.AsNoTracking().Where(tm=> tm.TeamId == teamId).ToListAsync();
        //     var teamJoinCode = await _db.TeamJoinCodes.AsNoTracking().FirstOrDefaultAsync(tjc => tjc.TeamId == teamId);

        //     TeamJoinCodeDto? teamJoinCodeDto = new TeamJoinCodeDto(teamJoinCode.JoinCode, teamJoinCode.CreatedAt, teamJoinCode.ExpiresAt); 

        //     if(teamJoinCode.ExpiresAt <= DateTime.UtcNow)
        //     {
        //         teamJoinCodeDto = null;
        //     }

        //     return new TeamDto
        //     (
        //         team.Id,team.Name,!team.NameAccepted,team.CreatedAt,
        //         teamMembers.Select(tm => new TeamMemberDto(tm.UserId, tm.Position)).ToList(),
        //         teamJoinCodeDto
        //     );
        // }
        
    } 
}