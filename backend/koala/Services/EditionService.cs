using koala.Data;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace koala.Services
{
    public class EditionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EditionService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<EditionInfoVM>> GetAllEditions()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Editions
                .AsNoTracking()
                .Select(e => new EditionInfoVM
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                })
                .ToListAsync();
        }

        public async Task<EditionInfoVM> AddEdition(EditionCreateVM newEdition)
        {
            using var context = await _factory.CreateDbContextAsync();

            var edition = new Edition
            {
                Id = Guid.NewGuid(),
                Title = newEdition.Title,
                StartDate = newEdition.StartDate,
                EndDate = newEdition.EndDate
            };

            context.Editions.Add(edition);
            await context.SaveChangesAsync();

            return new EditionInfoVM
            {
                Id = edition.Id,
                Title = edition.Title,
                StartDate = edition.StartDate,
                EndDate = edition.EndDate
            };
        }

        // Zmiana nazwy i sygnatury, aby idealnie pasowała do Twojego modelu tytułu
        public async Task<EditionInfoVM?> UpdateEditionTitle(EditionUpdateTitleVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var edition = await context.Editions.FirstOrDefaultAsync(e => e.Id == model.Id);
            if (edition == null) return null;

            edition.Title = model.Title;
            await context.SaveChangesAsync();

            return new EditionInfoVM
            {
                Id = edition.Id,
                Title = edition.Title,
                StartDate = edition.StartDate,
                EndDate = edition.EndDate
            };
        }

        // Dodatkowa metoda obsługująca aktualizację samej daty startu
        public async Task<EditionInfoVM?> UpdateEditionStartDate(EditionUpdateStartDateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var edition = await context.Editions.FirstOrDefaultAsync(e => e.Id == model.Id);
            if (edition == null) return null;

            edition.StartDate = model.StartDate;
            await context.SaveChangesAsync();

            return new EditionInfoVM
            {
                Id = edition.Id,
                Title = edition.Title,
                StartDate = edition.StartDate,
                EndDate = edition.EndDate
            };
        }

        // Dodatkowa metoda obsługująca aktualizację samej daty końca (konwersja DateTime -> DateTimeOffset)
        public async Task<EditionInfoVM?> UpdateEditionEndDate(EditionUpdateEndDateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var edition = await context.Editions.FirstOrDefaultAsync(e => e.Id == model.Id);
            if (edition == null) return null;

            // Twój model wymusza DateTime, konwertujemy go bezpiecznie na DateTimeOffset zachowując lokalną strefę
            edition.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Local);
            await context.SaveChangesAsync();

            return new EditionInfoVM
            {
                Id = edition.Id,
                Title = edition.Title,
                StartDate = edition.StartDate,
                EndDate = edition.EndDate
            };
        }

        // Poprawiono typ parametru wejściowego z int na Guid (zgodnie z bazą i EditionDeleteVM)
        public async Task<bool> DeleteEdition(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var edition = await context.Editions.FirstOrDefaultAsync(e => e.Id == id);
            if (edition == null) return false;

            context.Editions.Remove(edition);
            await context.SaveChangesAsync();
            return true;
        }
    }
}