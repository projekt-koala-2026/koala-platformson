using System.Security.Claims;
using System.Text.Json;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class StaticPageService
    {
        private readonly CmsDbContext _db;
        public StaticPageService(CmsDbContext db)
        {
            _db = db;
        }
        public async Task<StaticPageDto> UpdateStaticPagesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateStaticPageRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to update static page");
            }

            try
            {
                using var parsedContentJson = JsonDocument.Parse(requestDto.ContentJson);
            }
            catch (JsonException)
            {
                throw new KoalaException(KoalaErrorCodes.InvalidJson, "This is not a valid json");
            }

            int affectedRows = await _db.StaticPages
                .Where(x => x.Id == id && x.Version == requestDto.Version)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ContentJson, requestDto.ContentJson)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(x => x.Version, x => x.Version + 1));

            if (affectedRows == 0)
            {
                bool isStaticPage = await _db.StaticPages.AnyAsync(sp => sp.Id == id);
                if (!isStaticPage)
                {
                    throw new KoalaException(KoalaErrorCodes.StaticPageNotFound, "Specified static page does not exist");
                }
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }

            var staticPage = await _db.StaticPages.AsNoTracking().FirstOrDefaultAsync(sp => sp.Id == id);

            if (staticPage == null)
            {
                throw new KoalaException(KoalaErrorCodes.StaticPageNotFound, "Specified static page does not exist");
            }

            return new StaticPageDto(staticPage.Id, staticPage.Name, staticPage.ContentJson, staticPage.UpdatedAt, staticPage.Version);
        }
        public async Task<List<StaticPageDto>> GetStaticPagesAsync(ClaimsPrincipal? claimsPrincipal)
        {
            var staticPages = await _db.StaticPages.AsNoTracking()
                .Select
                (
                    sp => new StaticPageDto
                (
                    sp.Id,
                    sp.Name,
                    sp.ContentJson,
                    sp.UpdatedAt,
                    sp.Version
                )).ToListAsync();

            return staticPages;
        }
    }
}