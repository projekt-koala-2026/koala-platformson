using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Account.Services
{
    public class AccountService : IAccountModule
    {
        private readonly AccountDbContext _db;
        private readonly ICacheService _cache;
        public AccountService(AccountDbContext db, ICacheService cache)
        {
            _db = db;
            _cache = cache;
        }
        public async Task<_SessionResponseDto?> Internal_ValidateSessionFromPrincipalAsync(ClaimsPrincipal? claimsPrincipal)
        {
            Guid sessionToken = ClaimsHelper.GetSessionTokenGuid(claimsPrincipal);

            if (sessionToken == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing session token claim.");
            }

            var redisKey = $"koala:account:Session:{sessionToken}";

            // STRIKE CACHE
            var cachedSessionJson = await _cache.GetValueAtKeyAsync(redisKey);
            if (!string.IsNullOrEmpty(cachedSessionJson))
            {
                var cachedSession = JsonSerializer.Deserialize<_SessionResponseDto>(cachedSessionJson);
                if (cachedSession != null)
                {
                    if (cachedSession.ExpiresAt <= DateTimeOffset.UtcNow)
                    {
                        await _cache.RemoveKeyAsync(redisKey);
                        throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Session has expired in cache.");
                    }
                    return cachedSession;
                }
            }

            // STRIKE DATABASE
            var session = await _db.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == sessionToken && s.State == SessionStates.STATE_ACTIVE);

            if (session == null)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Session not found or expired in database.");
            }

            if (session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                session.State = SessionStates.STATE_INACTIVE;
                await _db.SaveChangesAsync();
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Session expired.");
            }

            // Get latest roles
            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == session.UserId)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();

            var sessionDto = new _SessionResponseDto(
                session.Id,
                session.Token,
                session.UserId,
                session.CreatedAt,
                session.ExpiresAt,
                userRolesNames,
                new UserDto
                (
                    session.User.Id,
                    session.User.NameFirst!,
                    session.User.NameLast!,
                    session.User.Email,
                    session.User.Censored,
                    userRolesNames,
                    session.User.CreatedAt,
                    session.User.UpdatedAt,
                    session.User.Version
                )
            );

            // IF FOUND IN DB AND IS VALID SESSION SAVE BACK TO REDIS
            TimeSpan timeSpanExpiry = session.ExpiresAt - DateTimeOffset.UtcNow;
            await _cache.SetValueAtKeyAsync(redisKey, JsonSerializer.Serialize(sessionDto), timeSpanExpiry);

            return sessionDto;
        }
        public async Task<_TeamResponseDto?> Internal_GetTeamInfo(Guid team_id)
        {
            return await _db.Teams.AsNoTracking()
                .Where(team => team.Id == team_id)
                .Select(team => new _TeamResponseDto(team.Id, team.EditionId, team.CaptainId, team.GuardianId, team.Tier, team.Name))
                .FirstOrDefaultAsync();
        }

        public async Task<List<_TeamResponseDto>> Internal_GetTeamsForEditionAsync(Guid editionId)
        {
            return await _db.Teams.AsNoTracking().Where(team => team.EditionId == editionId)
                .Select(team => new _TeamResponseDto(team.Id, team.EditionId, team.CaptainId, team.GuardianId, team.Tier, team.Name))
                .ToListAsync();
        }

        public async Task<List<Guid>> Internal_GetReviewerIdsAsync(List<Guid> userIds)
        {
            return await _db.UserRoles.AsNoTracking()
                .Where(role => userIds.Contains(role.UserId) && role.Role.Name == RoleNames.NAME_ORGANIZATION_REVIEWER && role.User.Verified)
                .Select(role => role.UserId).Distinct().ToListAsync();
        }
    }
}
