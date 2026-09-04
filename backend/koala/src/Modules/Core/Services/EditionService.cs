using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class EditionService
    {
        private readonly CoreDbContext _db;

        public EditionService(CoreDbContext db)
        {
            _db = db;
        }

        public async Task<EditionDto> CreateEdition(ClaimsPrincipal? claimsPrincipal, CreateEditionDto createEditionDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            bool isThereAnActiveEdition = _db.Editions.Where(e=> e.ExpiresAt == null).AsNoTracking().Any(); 

            if(isThereAnActiveEdition)
            {
                throw new CoreException(CoreErrorCodes.ActiveEditionAlreadyExists,"There is already an active edition, canot create a new one without ending last one first");
            }

            DateTime timeNow = DateTime.UtcNow;

            Edition edition = new Edition
            {
                Id = Guid.CreateVersion7(),
                Name = createEditionDto.Name,
                CreatedAt = timeNow,
                ExpiresAt = null
            };

            _db.Editions.Add(edition);
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<EditionDto> UpdateEdition(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateEditionNameDto updateEditionNameDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e=> e.Id == id); 

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"Edition of provided id does not exist");
            }

            edition.Name = updateEditionNameDto.Name;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<EditionDto> ExpireEdition(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e=> e.Id == id); 

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"Edition of provided id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.ExpiresAt = timeNow;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<EditionListDto> GetEditions(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            var queryResult = await _db.Editions.AsNoTracking().ToListAsync();
            var editions = queryResult
                .Skip(pageQueryDto.PageSize*pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new EditionDto
                (
                    e.Id,
                    e.Name,
                    e.CreatedAt,
                    e.ExpiresAt
                ))
                .ToList();

            return new EditionListDto(editions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task<EditionDto> GetActiveEdition(ClaimsPrincipal? claimsPrincipal)
        {
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.ExpiresAt == null);

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"There is no curent active edition at the moment");
            }

            return new EditionDto(edition.Id,edition.Name,edition.CreatedAt,edition.ExpiresAt);
        }
    }
}