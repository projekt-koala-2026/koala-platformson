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
    //FIXME: MAKE SURE THE SCHOOL CAN ONLY BE DELETED STARTING NEW EDITION
    //FIXME: SHOULD THIS PATH BE HARDCODED?
    public class SchoolService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly string _school_data_set_json;

        public SchoolService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
            _school_data_set_json = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_PATH");
            _school_data_set_json = Path.Combine(_school_data_set_json, "schools.json");
        }
        
        public async Task UpdateSchoolJsonFile()
        {
            //TODO: STREAM DATA HERE
        }

        public async Task ImportSchoolsFromCSV()
        {
            //TODO: FINISH THIS ENDPOINT
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
            await UpdateSchoolJsonFile();

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

    }
}
