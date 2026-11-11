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
    }
}