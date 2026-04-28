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
    public class EditionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EditionService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
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

            var editionInfoVM = new EditionInfoVM
            {
                Id = edition.Id,
                Title = edition.Title,
                StartDate = edition.StartDate,
                EndDate = edition.EndDate
            };

            return editionInfoVM;
        }
    }
}