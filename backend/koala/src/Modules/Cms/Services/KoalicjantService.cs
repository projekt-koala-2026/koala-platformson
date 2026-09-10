using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class KoalicjantService
    {
        private readonly CmsDbContext _db;
        public KoalicjantService(CmsDbContext db)
        {
            _db = db;
        }
        public async Task<KoalicjantDto> UpdateKoalicjantAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateKoalicjantRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            var koalicjant = await _db.Koalicjants.FirstOrDefaultAsync(k => k.Id == id);

            bool isNull = koalicjant == null;

            bool isSelf = false;
            
            if(!isNull)
            {
                isSelf = koalicjant.UserId.Equals(ClaimsHelper.GetUserGuid(claimsPrincipal));
            }

            if(!isSelf && !isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "User does not have permision");
            }

            if(isNull)
            {
                throw new CmsException(CmsErrorCodes.KoalicjantNotFound, "Could not find koalicjant with provided id");
            }

            DateTime timeNow = DateTime.UtcNow;

            koalicjant.NameFirst = requestDto.NameFirst;
            koalicjant.NameLast = requestDto.NameLast;
            koalicjant.Email = requestDto.Email;
            koalicjant.ContentJson = requestDto.ContentJson;
            koalicjant.IsVisiable = requestDto.IsVisiable;
            koalicjant.UpdatedAt = timeNow;
            koalicjant.Version = requestDto.Version;

            await _db.SaveChangesAsync();

            return new KoalicjantDto
            (
                koalicjant.Id,
                koalicjant.NameFirst,
                koalicjant.NameLast,
                koalicjant.Email,
                koalicjant.ContentJson,
                koalicjant.IsVisiable,
                koalicjant.UpdatedAt,
                koalicjant.Version
            );

        }
        public async Task<(List<KoalicjantDto>, ApiPagination)> GetKoalicjantsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            //TODO: ADD A WAY TO FILLTER OUT DATA
            //bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            //bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            var koalicjants = await _db.Koalicjants.AsNoTracking().Where(k => k.IsVisiable == true)
                .Select
                (
                    k => new KoalicjantDto
                    (
                        k.Id,
                        k.NameFirst,
                        k.NameLast,
                        k.Email,
                        k.ContentJson,
                        k.IsVisiable,
                        k.UpdatedAt,
                        k.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToListAsync();

            return (koalicjants , new ApiPagination(pageQueryDto.PageNumber,pageQueryDto.PageSize,await _db.Koalicjants.AsNoTracking().CountAsync()));
        }
    }
}