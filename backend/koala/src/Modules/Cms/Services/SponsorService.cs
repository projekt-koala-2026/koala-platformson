using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Org.BouncyCastle.Ocsp;

namespace koala.src.Modules.Cms.Services
{
    public class SponsorService
    {
        private readonly CmsDbContext _db;
        public SponsorService(CmsDbContext db)
        {
            _db = db;
        }
        public async Task<SponsorDto> CreateSponsorAsync(ClaimsPrincipal? claimsPrincipal, CreateSponsorRequestDto requestDto)
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
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            DateTime timeNow = DateTime.UtcNow;

            Sponsor sponsor = new Sponsor
            {
                Id = Guid.CreateVersion7(),
                Name = requestDto.Name,
                ContentJson = requestDto.ContentJson,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                IsVisible = requestDto.IsVisible,
                Version = 1
            };

            await _db.Sponsors.AddAsync(sponsor);
            await _db.SaveChangesAsync();

            return new SponsorDto(sponsor.Id, sponsor.Name, sponsor.ContentJson, sponsor.IsVisible, sponsor.UpdatedAt, sponsor.CreatedAt, sponsor.Version);
        }
        public async Task<SponsorDto> UpdateSponsorAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateSponsorRequestDto requestDto)
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
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            int affectedRows = await _db.Sponsors
                .Where(x => x.Id == id && x.Version == requestDto.Version)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Name, requestDto.Name)
                    .SetProperty(x => x.ContentJson, requestDto.ContentJson)
                    .SetProperty(x => x.IsVisible, requestDto.IsVisible)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(x => x.Version, x => x.Version + 1));

            if (affectedRows == 0)
            {
                bool isSponsor = await _db.Sponsors.AnyAsync(s => s.Id == id);
                if (!isSponsor)
                {
                    throw new KoalaException(KoalaErrorCodes.SponsorNotFound, "Specified sponsor does not exist");
                }
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }


            var sponsor = await _db.Sponsors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (sponsor == null)
            {
                throw new KoalaException(KoalaErrorCodes.SponsorNotFound, "Sponsor does not exist");
            }

            return new SponsorDto(sponsor.Id, sponsor.Name, sponsor.ContentJson, sponsor.IsVisible, sponsor.UpdatedAt, sponsor.CreatedAt, sponsor.Version);
        }
        public async Task DeleteSponsorAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
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
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var sponsor = await _db.Sponsors.FirstOrDefaultAsync(s => s.Id == id);

            if (sponsor == null)
            {
                throw new KoalaException(KoalaErrorCodes.SponsorNotFound, "Could not find sponsor for this id");
            }

            _db.Sponsors.Remove(sponsor);
            await _db.SaveChangesAsync();
        }
        public async Task<(List<SponsorDto>, ApiPagination)> GetSponsorsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            var query = _db.Sponsors.AsNoTracking().AsQueryable();

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(s => s.IsVisible == true);
            }

            var queryResults = await query.ToListAsync();
            var sponsors = queryResults
                .Select
                (
                    s => new SponsorDto
                    (
                        s.Id,
                        s.Name,
                        s.ContentJson,
                        s.IsVisible,
                        s.UpdatedAt,
                        s.CreatedAt,
                        s.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToList();

            return (sponsors, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }

        public async Task<SponsorDto> GetSponsorAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            var query = _db.Sponsors.AsNoTracking().AsQueryable();

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(s => s.IsVisible == true);
            }

            var queryResult = await query.FirstOrDefaultAsync(s => s.Id == id);
            var sponsor = new SponsorDto
                (
                    queryResult.Id,
                    queryResult.Name,
                    queryResult.ContentJson,
                    queryResult.IsVisible,
                    queryResult.UpdatedAt,
                    queryResult.CreatedAt,
                    queryResult.Version
                );

            return sponsor;
        }
    }
}
