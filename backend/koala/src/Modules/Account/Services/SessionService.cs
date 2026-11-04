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
    public interface ISessionService
    {
        public Task<_SessionResponseDto> CreateSessionAsync(LoginRequestDto requestDto);
        public Task DeleteSessionAsync(ClaimsPrincipal? claimsPrincipal, Guid session_id);
        public Task<(List<SessionDto>, ApiPagination)> GetSessionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto);
        public Task DeactivateSessionAsync(ClaimsPrincipal? claimsPrincipal, Guid session_id);
        public Task<(List<SessionDto>, ApiPagination)> GetActiveSessionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto);
        public Task DeleteSessionsAsync(ClaimsPrincipal? claimsPrincipal);
    }
    public class SessionService : ISessionService
    {
        private readonly AccountDbContext _db;
        private readonly ICacheService _cache;
        public SessionService(AccountDbContext db, ICacheService cache)
        {
            _db = db;
            _cache = cache;
        }
        public async Task<_SessionResponseDto> CreateSessionAsync(LoginRequestDto requestDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.InvalidCredentials, "Invalid credentials");
            }

            (bool Verified, bool NeedsRehash, string? NewHash) = PasswordHasher.VerifyAndMaybeRehash(user.PasswordHash!, requestDto.Password);

            if (!Verified)
            {
                throw new KoalaException(KoalaErrorCodes.InvalidCredentials, "Invalid credentials.");
            }

            if (NeedsRehash)
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
                UpdatedAt = timeNow,
                ExpiresAt = timeNow.AddHours(12.0f),
                State = SessionStates.STATE_ACTIVE
            };

            await _db.Sessions.AddAsync(session);
            await _db.SaveChangesAsync();

            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();

            var userData = new UserDto(user.Id, user.NameFirst!, user.NameLast!, user.Email, user.Censored, userRolesNames, user.CreatedAt, user.UpdatedAt, user.Version);
            return new _SessionResponseDto(session.Id, session.Token, user.Id, session.CreatedAt, session.ExpiresAt, userRolesNames, userData);
        }
        public async Task DeleteSessionAsync(ClaimsPrincipal? claimsPrincipal, Guid session_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing user id claim.");
            }

            var session = await _db.Sessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == session_id);

            if (session == null)
            {
                throw new KoalaException(KoalaErrorCodes.SessionNotFound, "Invalid session id");
            }

            await _cache.RemoveKeyAsync($"koala:account:Session:{session.Token}");
            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();
        }
        public async Task DeactivateSessionAsync(ClaimsPrincipal? claimsPrincipal, Guid session_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing user id claim.");
            }

            var session = await _db.Sessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == session_id && s.State == SessionStates.STATE_ACTIVE);

            if (session == null)
            {
                throw new KoalaException(KoalaErrorCodes.SessionNotFound, "Invalid session was already deactivated or does not exist");
            }

            await _cache.RemoveKeyAsync($"koala:account:Session:{session.Token}");
            session.State = SessionStates.STATE_INACTIVE;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        public async Task<(List<SessionDto>, ApiPagination)> GetSessionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing user id claim.");
            }

            var queryResults = await _db.Sessions.Where(s => s.UserId == userId).AsNoTracking().AsQueryable().ToListAsync();
            var sessions = queryResults
                .Select(s => new SessionDto(s.Id, s.UserId, s.State, s.CreatedAt, s.UpdatedAt, s.ExpiresAt))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return (sessions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
        public async Task<(List<SessionDto>, ApiPagination)> GetActiveSessionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing user id claim.");
            }

            var queryResults = await _db.Sessions.Where(s => s.UserId == userId && s.State == SessionStates.STATE_ACTIVE).AsNoTracking().AsQueryable().ToListAsync();
            var sessions = queryResults
                .Select(s => new SessionDto(s.Id, s.UserId, s.State, s.CreatedAt, s.UpdatedAt, s.ExpiresAt))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return (sessions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
        public async Task DeleteSessionsAsync(ClaimsPrincipal? claimsPrincipal)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Principal is null.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid or missing user id claim.");
            }

            var sessions = await _db.Sessions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var session in sessions)
            {
                await _cache.RemoveKeyAsync($"koala:account:Session:{session.Token}");
            }
            _db.Sessions.RemoveRange(sessions);
            await _db.SaveChangesAsync();
        }
    }
}
