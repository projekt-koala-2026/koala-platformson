using koala.Data;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace koala.Services
{
    //FIXME: DECIDE ON THE ENDPOINT STYLE (POST, PUT, GET OR DELETE) !!!
    //FIXME: MAKE SURE QUERIES ARE OPTIMAL AND AS SPECIFIC AS POSSIBLE !!!
    public class SponsorService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public SponsorService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<Sponsor> CreateAsync(SponsorCreateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            
            var sponsor = new Sponsor
            {
                Name = model.Name,
                LogoUrl = model.LogoUrl,
                WebsiteUrl = model.WebsiteUrl,
                Description = model.Description
            };

            context.Sponsors.Add(sponsor);
            await context.SaveChangesAsync();

            return sponsor;
        }

        public async Task<bool> UpdateAsync(Guid id, SponsorUpdateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var sponsor = await context.Sponsors.FindAsync(id);
            if (sponsor == null) return false;

            sponsor.Name = model.Name;
            sponsor.LogoUrl = model.LogoUrl;
            sponsor.WebsiteUrl = model.WebsiteUrl;
            sponsor.Description = model.Description;

            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var sponsor = await context.Sponsors.FindAsync(id);
            if (sponsor == null) return false;

            context.Sponsors.Remove(sponsor);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Sponsor?> GetByIdAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Sponsors.FindAsync(id);
        }

        public async Task<IEnumerable<Sponsor>> GetAllAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Sponsors.ToListAsync();
        }
    
    }
}