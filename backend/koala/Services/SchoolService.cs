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
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace koala.Services
{
    //FIXME: DECIDE ON THE ENDPOINT STYLE (POST, PUT, GET OR DELETE) !!!
    //FIXME: MAKE SURE THE SCHOOL CAN ONLY BE DELETED STARTING NEW EDITION
    //FIXME: SHOULD THIS PATH BE HARDCODED?

    public class SchoolService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        private static int ParseInt(string value)
        {
            value = value
                .Replace("=", "")
                .Replace("\"", "");

            return int.Parse(value);
        }

        private static string BuildAddress(string street, string buildingNumber, string apartmentNumber)
        {
            street ??= "";
            buildingNumber = buildingNumber?
                .Replace("=", "")
                .Replace("\"", "") ?? "";

            apartmentNumber = apartmentNumber?
                .Replace("=", "")
                .Replace("\"", "") ?? "";

            return string.IsNullOrWhiteSpace(apartmentNumber)
                ? $"{street} {buildingNumber}"
                : $"{street} {buildingNumber}/{apartmentNumber}";
        }

        public SchoolService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        //FIXME:
        //DODAĆ TRANSAKCJE DO BAZYDANYCH TUTAJ XD
        public async Task<int> ImportSchoolsFromCSV(SchoolFileCreate model)
        {
            if (model.File == null || model.File.Length == 0)
                return 0;

            using var context = await _factory.CreateDbContextAsync();

            using var stream = model.File.OpenReadStream();
            using var reader = new StreamReader(stream);

            var schools = new List<School>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var csv = new CsvReader(reader, config);


            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var school = new School
                {
                    RSPO = ParseInt(csv.GetField("Numer RSPO")),
                    Name = csv.GetField("Nazwa"),
                    NameShort = null,
                    State = csv.GetField("Województwo"),
                    City = csv.GetField("Miejscowość"),
                    Type = csv.GetField("Typ"),
                    Addres = BuildAddress(
                        csv.GetField("Ulica"),
                        csv.GetField("Numer budynku"),
                        csv.GetField("Numer lokalu"))
                };

                schools.Add(school);
            }

            await context.Schools.AddRangeAsync(schools);

            await context.SaveChangesAsync();
            
            Console.WriteLine($"Schools to add: {schools.Count}");

            return schools.Count;
        }
        
        public async Task<SchoolInfoVM> AddSchool(SchoolCreateVM newSchoolVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            using var context = await _factory.CreateDbContextAsync();
            
            var newSchool = context.Schools
            .FirstOrDefault(s => s.RSPO == newSchoolVM.RSPO);

            if (newSchool != null)
            {
                return null;
            }
            
            newSchool = new School
            {
                RSPO = newSchoolVM.RSPO,
                Name = newSchoolVM.Name,
                NameShort = newSchoolVM.NameShort,
                State = newSchoolVM.State,
                City = newSchoolVM.City,
                Type = newSchoolVM.Type,
                Addres = newSchoolVM.Addres 
            };

            context.Schools.Add(newSchool);
            await context.SaveChangesAsync();

            var schoolInfoVM = new SchoolInfoVM 
            {   
                RSPO = newSchool.RSPO,
                Name = newSchool.Name,
                NameShort = newSchool.NameShort,
                State = newSchool.State,
                City = newSchool.City,
                Type = newSchool.Type,
                Addres = newSchool.Addres 
            };

            return schoolInfoVM;
        }
        
        public async Task<SchoolInfoVM> EditSchoolName(SchoolEditNameVM editSchool)
        {
             using var context = await _factory.CreateDbContextAsync();
            var school = await context.Schools
                .FirstOrDefaultAsync(s => s.RSPO == editSchool.RSPO);

            if (school == null)
            {
                return null;
            }

            school.Name = editSchool.Name;
            await context.SaveChangesAsync();

            var schoolInfoVM = new SchoolInfoVM 
            {   
                RSPO = school.RSPO,
                Name = school.Name,
                NameShort = school.NameShort,
                State = school.State,
                City = school.City,
                Type = school.Type,
                Addres = school.Addres 
            };

            return schoolInfoVM;
        }

        public async Task<SchoolInfoVM> EditSchoolNameShort(SchoolEditNameShortVM editSchool)
        {
             using var context = await _factory.CreateDbContextAsync();
            var school = await context.Schools
                .FirstOrDefaultAsync(s => s.RSPO == editSchool.RSPO);

            if (school == null)
            {
                return null;
            }

            school.NameShort = editSchool.NameShort;
            await context.SaveChangesAsync();

            var schoolInfoVM = new SchoolInfoVM 
            {   
                RSPO = school.RSPO,
                Name = school.Name,
                NameShort = school.NameShort,
                State = school.State,
                City = school.City,
                Type = school.Type,
                Addres = school.Addres 
            };

            return schoolInfoVM;
        }

        public async Task DeleteSchool(SchoolDeleteVM deleteSchool)
        {
            using var context = await _factory.CreateDbContextAsync();
            var school = await context.Schools
                .FirstOrDefaultAsync(s => s.RSPO == deleteSchool.RSPO);

            if (school == null)
            {
                return;
            }

            var affectedRowsSchools = await context.Schools
                .Where(s => s.RSPO == school.RSPO)
                .ExecuteDeleteAsync();

            //TODO: CHECK WHAT HAPPEND WITH DELETION AND MAKE CORECT RETURN VALUES

            return;
        }

        public async Task DeleteSchools()
        {
            using var context = await _factory.CreateDbContextAsync();
            var school = await context.Schools.ToListAsync();

            await context.Schools.ExecuteDeleteAsync();

            //TODO: CHECK WHAT HAPPEND WITH DELETION AND MAKE CORECT RETURN VALUES

            return;
        }

        public async Task<List<SchoolInfoVM>> GetSchools()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Schools
                .Select(s => new SchoolInfoVM
                {
                    RSPO = s.RSPO,
                    Name = s.Name,
                    NameShort = s.NameShort,
                    State = s.State,
                    City = s.City,
                    Type = s.Type,
                    Addres = s.Addres
                })
                .ToListAsync();
        }

    }
}
