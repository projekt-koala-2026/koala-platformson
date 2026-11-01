using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class CoreService : ICoreModule
    {
        private readonly CoreDbContext _db;

        public CoreService(CoreDbContext db)
        {
            _db = db;
        }

        public async Task<_EditionDto?> Internal_GetTheActiveEditionAsync()
        {
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.State == EditionStates.STATE_ACTIVE);

            if (edition == null)
            {
                return null;
            }

            return new _EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<_SchoolDto?> Internal_GetSchoolAsync(Guid id)
        {
            var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (school == null)
            {
                return null;
            }

            return new _SchoolDto
            (
                school.Id,
                school.NameFull,
                school.NameShort,
                school.State,
                school.City,
                school.Road,
                school.Building,
                school.Rspo,
                school.Type,
                school.Tier,
                school.Email,
                school.CreatedAt,
                school.UpdatedAt,
                school.Version
            );
        }

        public async Task<(List<string>, ApiPagination)> Internal_GetSchoolsEmailsAsync(PageQueryDto pageQueryDto)
        {
            var schoolEmails = await _db.Schools
                .AsNoTracking()
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(s => s.Email)
                .ToListAsync();
            var schoolTotalCount = await _db.Schools.AsNoTracking().CountAsync();

            return (schoolEmails, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, schoolTotalCount));
        }
    }
}
