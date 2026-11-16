using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;

namespace koala.src.Modules.Account.Services
{
    public interface ILinkService
    {
        public Task ConsumeRegisterLinkAsync(Guid token, LinkConsumeRegisterRequestDto requestDto);
        public Task ConsumeResetPasswordLinkAsync(Guid token, LinkConsumeResetPasswordRequestDto requestDto);
        public Task GenerateResetPasswordLinkAsync(LinkGenerateResetPasswordRequestDto requestDto);
        public Task DeleteUserLinkAsync(ClaimsPrincipal? claimsPrincipal, Guid id);
        public Task DeactivateUserLinkAsync(ClaimsPrincipal? claimsPrincipal, Guid id);
        public Task<(List<LinkDto>, ApiPagination)> GetActiveUserLinksAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto);
        public Task<(List<LinkDto>, ApiPagination)> GetUserLinksAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto);
        public Task DeleteUserLinksAsync(ClaimsPrincipal? claimsPrincipal);
    }
    public class LinkService : ILinkService
    {
        private readonly AccountDbContext _db;
        private readonly IEmailService _emailService;
        public LinkService(AccountDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }
        public async Task ConsumeRegisterLinkAsync(Guid token, LinkConsumeRegisterRequestDto requestDto)
        {
            DateTime timeNow = DateTime.UtcNow;
            var link = await _db.Links.FirstOrDefaultAsync(l => (l.Token == token) && (l.State == LinkStates.STATE_ACTIVE) && (l.Type == LinkTypes.TYPE_REGISTRATION));
            if (link == null)
            {
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            if (link.ExpiresAt < timeNow)
            {
                link.State = LinkStates.STATE_INACTIVE;
                await _db.SaveChangesAsync();
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == link.UserId && u.Verified == false);
            if (user == null)
            {
                link.State = LinkStates.STATE_INACTIVE;
                await _db.SaveChangesAsync();
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User connected to that link no longer exists");
            }

            var rolesRequiring18 = new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR, RoleNames.NAME_ORGANIZATION_REVIEWER, RoleNames.NAME_TEAM_GUARDIAN };

            bool requires18 = user.UserRoles.Any(r => rolesRequiring18.Contains(r.Role.Name));
            if (requires18 && !requestDto.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "This registration link requires you to confirm you are at least 18 years old.");
            }

            bool isCaptain = user.UserRoles.Any(r => r.Role.Name == RoleNames.NAME_TEAM_CAPTAIN);
            if (isCaptain && !requestDto.DeclaredAtLeast13 && !requestDto.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "Captains must confirm they are at least 13 years old.");
            }

            link.State = LinkStates.STATE_INACTIVE;
            link.UpdatedAt = timeNow;
            user.NameFirst = requestDto.NameFirst;
            user.NameLast = requestDto.NameLast;
            user.PasswordHash = PasswordHasher.Hash(requestDto.Password);
            user.AcceptedRodo = requestDto.AcceptedRodo;
            user.AcceptedRules = requestDto.AcceptedRules;
            user.DeclaredAtLeast13 = requestDto.DeclaredAtLeast13;
            user.DeclaredAtLeast18 = requestDto.DeclaredAtLeast18;
            user.Verified = true;
            user.VerifiedAt = timeNow;

            await _db.SaveChangesAsync();
        }

        public async Task ConsumeResetPasswordLinkAsync(Guid token, LinkConsumeResetPasswordRequestDto requestDto)
        {
            DateTime timeNow = DateTime.UtcNow;
            var link = await _db.Links.FirstOrDefaultAsync(l => (l.Token == token) && (l.State == LinkStates.STATE_ACTIVE) && (l.Type == LinkTypes.TYPE_PASSWORD_RESET));
            if (link == null)
            {
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            if (link.ExpiresAt < timeNow)
            {
                link.State = LinkStates.STATE_INACTIVE;
                link.UpdatedAt = timeNow;
                await _db.SaveChangesAsync();
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == link.UserId && u.Verified == true);
            if (user == null)
            {
                link.State = LinkStates.STATE_INACTIVE;
                link.UpdatedAt = timeNow;
                await _db.SaveChangesAsync();
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User connected to that link no longer exists");
            }
            link.State = LinkStates.STATE_INACTIVE;
            link.UpdatedAt = timeNow;
            user.PasswordHash = PasswordHasher.Hash(requestDto.Password);
            await _db.SaveChangesAsync();
        }

        public async Task GenerateResetPasswordLinkAsync(LinkGenerateResetPasswordRequestDto requestDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email && u.Verified == true);
            if (user == null)
            {
                return;
            }

            DateTime timeNow = DateTime.UtcNow;

            var previousLinks = await _db.Links
                .Where(l => l.UserId == user.Id && l.Type == LinkTypes.TYPE_PASSWORD_RESET && l.State == LinkStates.STATE_ACTIVE)
                .ToListAsync();
            foreach (var previousLink in previousLinks)
            {
                previousLink.State = LinkStates.STATE_INACTIVE;
            }

            Link link = new Link
            {
                Id = Guid.CreateVersion7(),
                Token = Guid.NewGuid(),
                UserId = user.Id,
                Type = LinkTypes.TYPE_PASSWORD_RESET,
                State = LinkStates.STATE_ACTIVE,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                ExpiresAt = timeNow.AddDays(1.0f)
            };

            await _db.Links.AddAsync(link);
            await _db.SaveChangesAsync();

            var organizationAccount = await _db.UserRoles
                .AnyAsync(userRole =>
                    userRole.UserId == user.Id &&
                    (userRole.Role.Name == RoleNames.NAME_ORGANIZATION_ADMIN ||
                     userRole.Role.Name == RoleNames.NAME_ORGANIZATION_EDITOR ||
                     userRole.Role.Name == RoleNames.NAME_ORGANIZATION_REVIEWER));

            await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                link.Token.ToString(),
                organizationAccount ? "organization" : "team"
            );
        }

        public async Task<(List<LinkDto>, ApiPagination)> GetActiveUserLinksAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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

            var queryResult = await _db.Links.Where(l => l.State == LinkStates.STATE_ACTIVE && l.UserId == userId).AsNoTracking().AsQueryable().ToListAsync();
            var links = queryResult
                .Select(l => new LinkDto(l.Id, l.Type, l.State, l.CreatedAt, l.UpdatedAt, l.ExpiresAt))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return (links, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task<(List<LinkDto>, ApiPagination)> GetUserLinksAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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

            var queryResult = await _db.Links.Where(l => l.UserId == userId).AsNoTracking().AsQueryable().ToListAsync();
            var links = queryResult
                .Select(l => new LinkDto(l.Id, l.Type, l.State, l.CreatedAt, l.UpdatedAt, l.ExpiresAt))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return (links, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task DeactivateUserLinkAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var link = await _db.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId && l.State == LinkStates.STATE_ACTIVE);

            if (link == null)
            {
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "Link does not exist or was already deactivated");
            }

            link.State = LinkStates.STATE_INACTIVE;
            link.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return;
        }

        public async Task DeleteUserLinkAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var link = await _db.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (link == null)
            {
                throw new KoalaException(KoalaErrorCodes.LinkNotFound, "Link does not exist");
            }

            _db.Links.Remove(link);
            await _db.SaveChangesAsync();

            return;
        }

        public async Task DeleteUserLinksAsync(ClaimsPrincipal? claimsPrincipal)
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

            var links = await _db.Links.Where(l => l.UserId == userId).ToListAsync();
            _db.Links.RemoveRange(links);
            await _db.SaveChangesAsync();

            return;
        }
    }
}
