using koala.src.Modules.Core.Data;
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
                throw new CoreException(CoreErrorCodes.ActiveEditionNotFound,"There is no curent active edition at the moment");
            }

            return new _EditionDto(edition.Id,edition.Name,edition.CreatedAt,edition.ExpiresAt);
        }
    }
}