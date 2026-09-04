using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
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
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User must be valid authenticated");
            }

            Guid sessionToken = ClaimsHelper.GetSessionTokenGuid(claimsPrincipal);

            if (sessionToken == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid or missing session token claim.");
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
                        throw new AccountException(AccountErrorCodes.Unauthenticated,"Session has expired in cache.");
                    }
                    return cachedSession;
                }
            }
        
            // STRIKE DATABASE
            var session = await _db.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == sessionToken && s.Active);
        
            if (session == null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Session not found or expired in database.");
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
                    userRolesNames
                )
            );
        
            // IF FOUND IN DB AND IS VALID SESSION SAVE BACK TO REDIS
            TimeSpan timeSpanExpiry = session.ExpiresAt - DateTimeOffset.UtcNow;
            await _cache.SetValueAtKeyAsync(redisKey, JsonSerializer.Serialize(sessionDto), timeSpanExpiry);
        
            return sessionDto;
        }
        public async Task Internal_GetUsersByIdsAsync(){}
    }
}