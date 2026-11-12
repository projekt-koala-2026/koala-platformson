using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class KoalicjantService
    {
        private readonly CmsDbContext _db;
        private readonly IAccountModule _accountModule;
        public KoalicjantService(CmsDbContext db, IAccountModule accountModule)
        {
            _db = db;
            _accountModule = accountModule;
        }

        public async Task<KoalicjantDto> AddKoalicjantAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpsertKoalicjantRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            bool isValidKoalicjantId = await _accountModule.Internal_IsOrganizationUser(id);

            if (!isValidKoalicjantId)
            {
                throw new KoalaException(KoalaErrorCodes.KoalicjantNotFound, "Invalid id there is no koalicjant associeted with it");
            }

            bool isSelf = id == ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isSelf && !isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission.");
            }

            var koalicjant = await _db.Koalicjants.FirstOrDefaultAsync(k => k.Id == id);
            if (koalicjant != null)
            {
                throw new KoalaException(KoalaErrorCodes.KoalicjantAlreadyExists, "Koalicjant for this id already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            koalicjant = new Koalicjant
            {
                Id = id,
                NameFirst = requestDto.NameFirst,
                NameLast = requestDto.NameLast,
                Email = requestDto.Email,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                IsVisible = requestDto.IsVisible,
                ContentJson = requestDto.ContentJson,
                Version = 1
            };

            await _db.Koalicjants.AddAsync(koalicjant);

            return new KoalicjantDto
            (
                koalicjant.Id,
                koalicjant.NameFirst,
                koalicjant.NameLast,
                koalicjant.Email,
                koalicjant.ContentJson,
                koalicjant.IsVisible,
                koalicjant.CreatedAt,
                koalicjant.UpdatedAt,
                koalicjant.Version
            );
        }
        public async Task<KoalicjantDto> UpdateKoalicjantAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpsertKoalicjantRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            bool isValidKoalicjantId = await _accountModule.Internal_IsOrganizationUser(id);

            if (!isValidKoalicjantId)
            {
                throw new KoalaException(KoalaErrorCodes.KoalicjantNotFound, "Invalid id there is no koalicjant associeted with it");
            }

            bool isSelf = id == ClaimsHelper.GetUserGuid(claimsPrincipal);

            if (!isSelf && !isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission.");
            }

            int affectedRows = await _db.Koalicjants
                .Where(x => x.Id == id && x.Version == requestDto.Version)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.NameFirst, requestDto.NameFirst)
                    .SetProperty(x => x.NameLast, requestDto.NameLast)
                    .SetProperty(x => x.Email, requestDto.Email)
                    .SetProperty(x => x.ContentJson, requestDto.ContentJson)
                    .SetProperty(x => x.IsVisible, requestDto.IsVisible)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(x => x.Version, x => x.Version + 1));

            if (affectedRows == 0)
            {
                bool isKoalicjant = await _db.Koalicjants.AnyAsync(s => s.Id == id);
                if (!isKoalicjant)
                {
                    throw new KoalaException(KoalaErrorCodes.KoalicjantNotFound, "Specified post does not exist");
                }
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }


            var koalicjant = await _db.Koalicjants.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (koalicjant == null)
            {
                throw new KoalaException(KoalaErrorCodes.KoalicjantNotFound, "Post does not exist");
            }

            return new KoalicjantDto
            (
                koalicjant.Id,
                koalicjant.NameFirst,
                koalicjant.NameLast,
                koalicjant.Email,
                koalicjant.ContentJson,
                koalicjant.IsVisible,
                koalicjant.CreatedAt,
                koalicjant.UpdatedAt,
                koalicjant.Version
            );

        }
        public async Task<(List<KoalicjantDto>, ApiPagination)> GetKoalicjantsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            var query = _db.Koalicjants.AsNoTracking().AsQueryable();

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(k => k.IsVisible == true);
            }

            var queryResults = await query.ToListAsync();
            var koalicjants = queryResults
                .Select
                (
                    k => new KoalicjantDto
                    (
                        k.Id,
                        k.NameFirst,
                        k.NameLast,
                        k.Email,
                        k.ContentJson,
                        k.IsVisible,
                        k.CreatedAt,
                        k.UpdatedAt,
                        k.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToList();

            return (koalicjants, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }

        public async Task<KoalicjantDto> GetKoalicjantAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            var query = _db.Koalicjants.AsNoTracking().AsQueryable();

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(k => k.IsVisible == true);
            }

            var queryResult = await query.FirstOrDefaultAsync(k => k.Id == id);
            var koalicjant = new KoalicjantDto
                (
                    queryResult.Id,
                    queryResult.NameFirst,
                    queryResult.NameLast,
                    queryResult.Email,
                    queryResult.ContentJson,
                    queryResult.IsVisible,
                    queryResult.CreatedAt,
                    queryResult.UpdatedAt,
                    queryResult.Version
                );

            return koalicjant;
        }
    }
}
