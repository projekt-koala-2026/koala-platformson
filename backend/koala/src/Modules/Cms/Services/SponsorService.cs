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
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            DateTime timeNow = DateTime.UtcNow;

            Sponsor sponsor = new Sponsor
            {
                Id = Guid.CreateVersion7(),
                Name = requestDto.Name,
                ContentJson = requestDto.ContentJson,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                IsVisiable = requestDto.IsVisable,
                Version = requestDto.Version
            };

            await _db.Sponsors.AddAsync(sponsor);
            await _db.SaveChangesAsync();

            return new SponsorDto(sponsor.Id,sponsor.Name,sponsor.ContentJson,sponsor.IsVisiable,sponsor.UpdatedAt,sponsor.CreatedAt,sponsor.Version);
        }
        public async Task<SponsorDto> UpdateSponsorAsync(ClaimsPrincipal? claimsPrincipal,Guid id, UpdateSponsorRequestDto requestDto)
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
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            var sponsor = await _db.Sponsors.FirstOrDefaultAsync(s => s.Id == id);

            if(sponsor == null)
            {
                throw new CmsException(CmsErrorCodes.SponsorNotFound, "Could not found sponsor for this id");
            }

            DateTime timeNow = DateTime.UtcNow;
            
            sponsor.Name = requestDto.Name;
            sponsor.ContentJson = requestDto.ContentJson;
            sponsor.IsVisiable = requestDto.IsVisiable;
            sponsor.Version = requestDto.Version;
            sponsor.UpdatedAt = timeNow;

            await _db.SaveChangesAsync();

            return new SponsorDto(sponsor.Id,sponsor.Name,sponsor.ContentJson,sponsor.IsVisiable,sponsor.UpdatedAt,sponsor.CreatedAt,sponsor.Version);
        }
        public async Task DeleteSponsorAsync(ClaimsPrincipal? claimsPrincipal,Guid id)
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
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            var sponsor = await _db.Sponsors.FirstOrDefaultAsync(s => s.Id == id);

            if(sponsor == null)
            {
                throw new CmsException(CmsErrorCodes.SponsorNotFound, "Could not found sponsor for this id");
            }
            
            _db.Sponsors.Remove(sponsor);
            await _db.SaveChangesAsync();
        }
        public async Task<(List<SponsorDto>, ApiPagination)> GetSponsorsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            var query = _db.Sponsors.AsNoTracking().AsQueryable();

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(s => s.IsVisiable == true);
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
                        s.IsVisiable,
                        s.UpdatedAt,
                        s.CreatedAt,
                        s.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToList();

            return (sponsors , new ApiPagination(pageQueryDto.PageNumber,pageQueryDto.PageSize,queryResults.Count));
        }

        public async Task<SponsorDto> GetSponsorAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            var query = _db.Sponsors.AsNoTracking().AsQueryable();

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(s => s.IsVisiable == true);
            }

            var queryResult = await query.FirstOrDefaultAsync(s => s.Id == id);
            var sponsor = new SponsorDto
                (
                    queryResult.Id,
                    queryResult.Name,
                    queryResult.ContentJson,
                    queryResult.IsVisiable,
                    queryResult.UpdatedAt,
                    queryResult.CreatedAt,
                    queryResult.Version
                );

            return sponsor;
        }
    }
}