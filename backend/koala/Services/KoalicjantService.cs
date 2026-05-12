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
    public class KoalicjantService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public KoalicjantService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<Koalicjant> CreateAsync(KoalicjantCreateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            
            var koalicjant = new Koalicjant
            {
                Name = model.Name,
                ProfilePicture = model.ProfilePicture,
                Description = model.Description
            };

            context.Koalicjanci.Add(koalicjant);
            await context.SaveChangesAsync();

            return koalicjant;
        }

        public async Task<bool> UpdateAsync(Guid id, KoalicjantUpdateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var koalicjant = await context.Koalicjanci.FindAsync(id);
            if (koalicjant == null) return false;

            koalicjant.Name = model.Name;
            koalicjant.ProfilePicture = model.ProfilePicture;
            koalicjant.Description = model.Description;

            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var koalicjant = await context.Koalicjanci.FindAsync(id);
            if (koalicjant == null) return false;

            context.Koalicjanci.Remove(koalicjant);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Koalicjant?> GetByIdAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Koalicjanci.FindAsync(id);
        }
    
    }
}