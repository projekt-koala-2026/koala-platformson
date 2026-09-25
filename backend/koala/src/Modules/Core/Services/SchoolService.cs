using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace koala.src.Modules.Core.Services
{
    public class SchoolService
    {
        private readonly CoreDbContext _db;

        public SchoolService(CoreDbContext db)
        {
            _db = db;
        }

        public async Task<SchoolDto> CreateSchoolAsync(ClaimsPrincipal? claimsPrincipal, CreateSchoolRequestDto createSchoolRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            //TODO check here if that all that is uniqe???
            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Rspo == createSchoolRequestDto.Rspo);

            if(school != null)
            {
                throw new CoreException(CoreErrorCodes.SchoolAlreadyExists, "Schoold with this rspo or full name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;
            school = new School
            {
                Id = Guid.CreateVersion7(),
                NameFull = createSchoolRequestDto.NameFull,
                NameShort = createSchoolRequestDto.NameShort,
                State = createSchoolRequestDto.State,
                City = createSchoolRequestDto.City,
                Road = createSchoolRequestDto.Road,
                Building = createSchoolRequestDto.Building,
                Rspo = createSchoolRequestDto.Rspo,
                Type = createSchoolRequestDto.Type,
                Email = createSchoolRequestDto.Email,
                CreatedAt = timeNow,
                UpdatedAt = timeNow
            };

            await _db.Schools.AddAsync(school);
            await _db.SaveChangesAsync();

            return new SchoolDto
            (
                school.Id,
                school.NameFull,
                school.NameShort,
                school.State,
                school.City,
                school.Road,
                school.Building,
                school.Rspo,
                school.Type,
                school.Email,
                school.CreatedAt,
                school.UpdatedAt
            );

        }

        public async Task<SchoolDto> UpdateSchoolAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateSchoolRequestDto updateSchoolRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == id);

            if(school == null)
            {
                throw new CoreException(CoreErrorCodes.SchoolNotFound, "Schoold with this id does not exist");
            }
            DateTime timeNow = DateTime.UtcNow;
            school.NameFull = updateSchoolRequestDto.NameFull;
            school.NameShort = updateSchoolRequestDto.NameShort;
            school.State = updateSchoolRequestDto.State;
            school.City = updateSchoolRequestDto.City;
            school.Road = updateSchoolRequestDto.Road;
            school.Building = updateSchoolRequestDto.Building;
            school.Rspo = updateSchoolRequestDto.Rspo;
            school.Type = updateSchoolRequestDto.Type;
            school.Email = updateSchoolRequestDto.Email;
            school.UpdatedAt = timeNow;

            await _db.SaveChangesAsync();

            return new SchoolDto
            (
                school.Id,
                school.NameFull,
                school.NameShort,
                school.State,
                school.City,
                school.Road,
                school.Building,
                school.Rspo,
                school.Type,
                school.Email,
                school.CreatedAt,
                school.UpdatedAt
            );
        }

        public async Task DeleteSchoolAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == id);

            if(school == null)
            {
                throw new CoreException(CoreErrorCodes.SchoolNotFound, "Schoold with this id does not exist");
            }

            _db.Schools.Remove(school);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSchoolsAsync(ClaimsPrincipal? claimsPrincipal, List<Guid> ids)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }

            var schools = await _db.Schools.Where(s => ids.Contains(s.Id)).ToListAsync();

            if(schools.Count == 0)
            {
                throw new CoreException(CoreErrorCodes.SchoolNotFound, "Not a single school with provided ids exists");
            }

            _db.Schools.RemoveRange(schools);
            await _db.SaveChangesAsync();
        }

        public async Task<int> ImportSchoolsAsync(ClaimsPrincipal? claimsPrincipal, ImportSchoolRequestDto importSchoolRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbiden,"User does not have permision to peform this operation on resource");
            }
            
            var file = importSchoolRequestDto.File; 
            if (file == null || file.Length == 0)
                return 0;

            using var stream = file.OpenReadStream();
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

            var expectedHeaders = new[] { "Numer RSPO", "Nazwa", "Województwo", "Miejscowość", "Ulica", "Numer budynku", "Typ" };
            var actualHeaders = csv.HeaderRecord;
        
            if (actualHeaders == null || !expectedHeaders.All(h => actualHeaders.Contains(h)))
            {
                throw new CoreException(CoreErrorCodes.InvalidImportSchoolFile, "The uploaded CSV file is missing one or more required columns.");
            }

            while (await csv.ReadAsync())
            {
                var school = new School
                {
                    Id = Guid.NewGuid(),
                    Rspo = csv.GetField("Numer RSPO")!,          // Mapped to string Rspo
                    NameFull = csv.GetField("Nazwa")!,           // Mapped to NameFull
                    NameShort = "",                             // null by default
                    State = csv.GetField("Województwo")!,        // Mapped to State
                    City = csv.GetField("Miejscowość")!,         // Mapped to City
                    Road = csv.GetField("Ulica")!,               // Mapped to separate Road column
                    Building = csv.GetField("Numer budynku")!,   // Mapped to separate house column
                    Type = csv.GetField("Typ")!,                 // Mapped to Type
                    Email = string.Empty,                       // Default/Empty if not in CSV
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                schools.Add(school);
            }

            await _db.Schools.AddRangeAsync(schools);
            await _db.SaveChangesAsync();

            return schools.Count;
        }

        public async Task<SchoolDto> GetSchoolAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            //CHECK IF WE CAN GIVE PUBLIC ACCESS TO SCHOOLS DATA ?
            var query = _db.Schools.AsNoTracking().AsQueryable();

            var queryResult = await query.FirstOrDefaultAsync(s => s.Id == id);

            if(queryResult == null)
            {
                throw new CoreException(CoreErrorCodes.SchoolNotFound, "Could not found a school with this id");
            }

            return new SchoolDto
            (
                queryResult.Id,
                queryResult.NameFull,
                queryResult.NameShort,
                queryResult.State,
                queryResult.City,
                queryResult.Road,
                queryResult.Building,
                queryResult.Rspo,
                queryResult.Type,
                queryResult.Email,
                queryResult.CreatedAt,
                queryResult.UpdatedAt
            );
        }

        public async Task<(List<SchoolDto>,ApiPagination)> GetSchoolsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            //CHECK IF WE CAN GIVE PUBLIC ACCESS TO SCHOOLS DATA ?
            var query = _db.Schools.AsNoTracking().AsQueryable();

            var queryResult = await query.ToListAsync();

            var schools = queryResult.Select(s => new SchoolDto
                (
                    s.Id,
                    s.NameFull,
                    s.NameShort,
                    s.State,
                    s.City,
                    s.Road,
                    s.Building,
                    s.Rspo,
                    s.Type,
                    s.Email,
                    s.CreatedAt,
                    s.UpdatedAt
                )).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize).ToList();

            return (schools, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }
    }
}