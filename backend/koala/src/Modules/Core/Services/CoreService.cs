using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
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

        public async Task<_EditionDto?> Internal_GetTheActiveEdition()
        {
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.ExpiresAt == null);

            if(edition == null)
            {
<<<<<<< HEAD
                throw new CoreException(CoreErrorCodes.ActiveEditionNotFound,"There is no current active edition at the moment");
=======
                //throw new CoreException(CoreErrorCodes.ActiveEditionNotFound,"There is no curent active edition at the moment");
                return null;
>>>>>>> origin/backend_refactor
            }

            return new _EditionDto(edition.Id,edition.Name,edition.CreatedAt,edition.ExpiresAt);
        }

        public async Task<bool> Internal_ExistSchool(Guid id)
        {
            var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if(school == null)
            {
                //throw new CoreException(CoreErrorCodes.SchoolNotFound,"There is no curent active edition at the moment");
                return false;
            }

            return true;
        }
    }
}