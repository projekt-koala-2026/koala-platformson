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
        public async Task<StaticPageDto> UpdateStaticPagesAsync(ClaimsPrincipal? claimsPrincipal,Guid id, UpdateStaticPageRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "User does not have permission to update static page");
            }

            var staticPage = await _db.StaticPages.FirstOrDefaultAsync(sp => sp.Id == id);

            if(staticPage == null)
            {
                throw new CmsException(CmsErrorCodes.StaticPageNotFound, "Specified static page does not exist");
            }

            try
            {
                using var parsedContentJson = JsonDocument.Parse(requestDto.ContentJson);
            }
            catch (JsonException)
            {
                throw new CmsException(CmsErrorCodes.InvalidJsonStructure, "This is not a valid json");
            }

            DateTime timeNow = DateTime.UtcNow;

            staticPage.ContentJson = requestDto.ContentJson;
            staticPage.Version = requestDto.Version;
            staticPage.UpdatedAt = timeNow;

            await _db.SaveChangesAsync();

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