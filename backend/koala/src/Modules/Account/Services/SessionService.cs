using System.Security.Authentication;
using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Account.Services
{
    public class SessionService
    {
        private readonly AccountDbContext _db;
        private readonly ICacheService _cache;
        public SessionService(AccountDbContext db, ICacheService cache)
        {
            _db = db;
            _cache = cache;
        }
        public async Task<_SessionResponseDto?> CreateSessionAsync(LoginRequestDto requestDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"Did not found a user for this email");
            }

            (bool Verified, bool NeedsRehash, string? NewHash) = PasswordHasher.VerifyAndMaybeRehash(user.PasswordHash!, requestDto.Password);

            if(!Verified)
            {
                throw new AccountException(AccountErrorCodes.IncorectPassword,"Incorect password");
            }

            if(NeedsRehash)
            {
                user.PasswordHash = NewHash;
            }
            
            DateTime timeNow = DateTime.UtcNow;
            Session session = new Session
            {
                Id = Guid.CreateVersion7(),
                Token = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = timeNow,
                ExpiresAt = timeNow.AddHours(2.0f),
                Active = true
            };
            
            await _db.Sessions.AddAsync(session);
            await _db.SaveChangesAsync();

            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();

            var userData = new UserDto(user.Id, user.NameFirst!, user.NameLast!, user.Email, user.Censored, userRolesNames);
            return new _SessionResponseDto(session.Id, session.Token, user.Id, session.CreatedAt, session.ExpiresAt, userRolesNames, userData);
        }
        public async Task DeleteSessionAsync(ClaimsPrincipal? claimsPrincipal, Guid session_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid or missing user id claim.");
            }

            var session = await _db.Sessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == session_id && s.Active == true);

            if(session == null)
            {
                throw new AccountException(AccountErrorCodes.SessionNotFound,"Invalid session id");
            }

            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();
        }
        public async Task<SessionListDto> GetSessionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid or missing user id claim.");
            }

            var queryResults = await _db.Sessions.Where(s => s.UserId == userId && s.Active == true).AsNoTracking().AsQueryable().ToListAsync();
            var sessions = queryResults
                .Select(s => new SessionDto(s.Id, s.UserId, s.CreatedAt, s.ExpiresAt))
                .Skip(pageQueryDto.PageSize*pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return new SessionListDto(sessions, new ApiPagination(pageQueryDto.PageNumber,pageQueryDto.PageSize,queryResults.Count));
        }
        public async Task DeleteSessionsAsync(ClaimsPrincipal? claimsPrincipal)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid or missing user id claim.");
            }

            var sessions = await _db.Sessions
                .Where(s => s.UserId == userId && s.Active == true)
                .ToListAsync();

            if(sessions == null)
            {
                throw new AccountException(AccountErrorCodes.SessionNotFound,"No sessions exist for user");
            }

            _db.Sessions.RemoveRange(sessions);
            await _db.SaveChangesAsync();
        }
    }
}