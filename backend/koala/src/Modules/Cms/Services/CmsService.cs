using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using koala.src.Shared.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class CmsService : ICmsModule
    {
        private readonly CmsDbContext _db;
        public CmsService(CmsDbContext db)
        {
            _db = db;
        }
        public async Task Internal_DeleteKoalicjantIfExistsAsync(Guid id)
        {
            var koalicjant = await _db.Koalicjants.Where(k => k.Id == id).ExecuteDeleteAsync();
            return;
        }

        public async Task Internal_CreateKoalicjantIfNotExistsAsync(Guid id, string NameFirst, string NameLast, string Email)
        {
            var koalicjant = await _db.Koalicjants.FirstOrDefaultAsync(k => k.Id == id);
            if(koalicjant != null)
            {
                return;
            }
            DateTime timeNow = DateTime.UtcNow;
            koalicjant = new Koalicjant
            {
                Id = id,
                NameFirst = NameFirst,
                NameLast = NameLast,
                Email = Email,
                IsVisible = false,
                ContentJson = "{}",
                CreatedAt = timeNow,
                UpdatedAt= timeNow,
                Version = 1
            };
            _db.Koalicjants.Add(koalicjant);
            await _db.SaveChangesAsync();
        }
    }
}
