using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Account.Services
{
    public class LinkService
    {
        private readonly AccountDbContext _db;
        private readonly EmailService _emailService;
        public LinkService(AccountDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }
        public async Task ConsumeRegisterLink(Guid token, ConsumeRegisterLinkRequestDto requestDto)
        {
            DateTime timeNow = DateTime.UtcNow;
            var link = await _db.Links.FirstOrDefaultAsync(l => (l.Token == token) && (l.Active == true) && (l.Type == "REGISTER"));
            if(link == null)
            {
                throw new AccountException(AccountErrorCodes.LinkNotFound,"The verification link with this token does not exist or is expired");
            }
            if(link.ExpiresAt < timeNow)
            {
                link.Active = false;
                await _db.SaveChangesAsync();
                throw new AccountException(AccountErrorCodes.LinkNotFound,"The verification link with this token does not exist or is expired");
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == link.UserId && u.Verified == false);
            if(user == null)
            {
                link.Active = false;
                await _db.SaveChangesAsync();
                throw new AccountException(AccountErrorCodes.UserNotFound,"User conected to that link no longer exists");
            }
            link.Active = false;
            //TODO: HASH THE PASSWORD
            user.NameFirst = requestDto.NameFirst;
            user.NameLast = requestDto.NameLast;
            user.PasswordHash = PasswordHasher.Hash(requestDto.Password);
            user.AcceptedRodo = requestDto.AcceptedRodo;
            user.AcceptedRules = requestDto.AcceptedRules;
            user.Verified = true;
            user.VerifiedAt = timeNow;

            await _db.SaveChangesAsync();
        }

        public async Task ConsumeResetPasswordLink(Guid token, ConsumeResetPasswordLinkRequestDto requestDto)
        {
            DateTime timeNow = DateTime.UtcNow;
            var link = await _db.Links.FirstOrDefaultAsync(l => (l.Token == token) && (l.Active == true) && (l.Type == "RESETPASSWORD"));
            if(link == null)
            {
                throw new AccountException(AccountErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            if(link.ExpiresAt < timeNow)
            {
                link.Active = false;
                await _db.SaveChangesAsync();
                throw new AccountException(AccountErrorCodes.LinkNotFound, "The verification link with this token does not exist or is expired");
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == link.UserId && u.Verified == true);
            if(user == null)
            {
                link.Active = false;
                await _db.SaveChangesAsync();
                throw new AccountException(AccountErrorCodes.UserNotFound,"User conected to that link no longer exists");
            }
            link.Active = false;
            //TODO: HASH THE PASSWORD
            user.PasswordHash = PasswordHasher.Hash(requestDto.Password);
            await _db.SaveChangesAsync();
        }

        public async Task GenerateResetPasswordLink(GenerateResetPasswordLinkRequestDto requestDto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            Link link = new Link
            {
                Id = Guid.CreateVersion7(),
                Token = Guid.NewGuid(),
                UserId = user.Id,
                Type = "RESETPASSWORD",
                Active = true,
                CreatedAt = timeNow,
                ExpiresAt = timeNow.AddDays(1.0f)
            };

            await _db.Links.AddAsync(link);
            await _db.SaveChangesAsync();

            //SEND TO EMAIL FIXME:
            await _emailService.SendPasswordResetEmailAsync(user.Email, link.Token.ToString());
        }

        public async Task<LinkListDto> GetActiveUserLinks(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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

            var queryResult = await _db.Links.Where(l=> l.Active == true && l.UserId == userId).AsNoTracking().AsQueryable().ToListAsync();
            var links = queryResult
                .Select(l=> new LinkDto(l.Id,l.Type,l.CreatedAt,l.ExpiresAt))
                .Skip(pageQueryDto.PageSize*pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return new LinkListDto(links, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task DeleteUserLink(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var link = await _db.Links.FirstOrDefaultAsync(l=> l.Id == id && l.UserId == userId && l.Active==true);
            
            if(link == null)
            {
                throw new AccountException(AccountErrorCodes.LinkNotFound,"Link does not exist");
            }

            _db.Links.Remove(link);
            await _db.SaveChangesAsync();

            return;
        }

        public async Task DeleteUserLinks(ClaimsPrincipal? claimsPrincipal)
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

            var links = await _db.Links.Where(l=> l.UserId == userId && l.Active==true).ToListAsync();
            
            if(links == null)
            {
                throw new AccountException(AccountErrorCodes.LinkNotFound,"Nothing to delete");
            }

            _db.Links.RemoveRange(links);
            await _db.SaveChangesAsync();

            return;
        }
    }
}