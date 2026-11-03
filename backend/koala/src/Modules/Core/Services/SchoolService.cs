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

        private static readonly Dictionary<string, string> TypeToTierMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // TIER 1
            { SchoolTypes.TYPE_1_TIER_1, SchoolTiers.TIER_1 },
            { SchoolTypes.TYPE_2_TIER_1, SchoolTiers.TIER_1 },
            { SchoolTypes.TYPE_3_TIER_1, SchoolTiers.TIER_1 },

            // TIER 2
            { SchoolTypes.TYPE_1_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_2_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_3_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_4_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_5_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_6_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_7_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_8_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_9_TIER_2, SchoolTiers.TIER_2 },
            { SchoolTypes.TYPE_10_TIER_2, SchoolTiers.TIER_2 },
        };

        private static bool SchoolValidateTypeAndGetTier(string schoolType, out string tier)
        {
            return TypeToTierMap.TryGetValue(schoolType?.Trim() ?? string.Empty, out tier!);
        }

        public async Task<SchoolDto> CreateSchoolAsync(ClaimsPrincipal? claimsPrincipal, SchoolCreateRequestDto schoolCreateRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            //TODO check here if that all that is unique???
            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Rspo == schoolCreateRequestDto.Rspo);

            if (school != null)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolAlreadyExists, "School with this rspo or full name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (!SchoolValidateTypeAndGetTier(schoolCreateRequestDto.Type, out string tier))
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "Invalid school type");
            }

            school = new School
            {
                Id = Guid.CreateVersion7(),
                NameFull = schoolCreateRequestDto.NameFull,
                NameShort = schoolCreateRequestDto.NameShort,
                State = schoolCreateRequestDto.State,
                City = schoolCreateRequestDto.City,
                Road = schoolCreateRequestDto.Road,
                Building = schoolCreateRequestDto.Building,
                Rspo = schoolCreateRequestDto.Rspo,
                Type = schoolCreateRequestDto.Type,
                Tier = tier,
                Email = schoolCreateRequestDto.Email,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
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
                school.Tier,
                school.Email,
                school.CreatedAt,
                school.UpdatedAt,
                school.Version
            );

        }

        public async Task<SchoolDto> UpdateSchoolAsync(ClaimsPrincipal? claimsPrincipal, Guid id, SchoolUpdateRequestDto schoolUpdateRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == id);

            if (school == null)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "School with this id does not exist");
            }

            if (school.Version != schoolUpdateRequestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Updated school version does not match make sure u have the freshest data");
            }

            if (!SchoolValidateTypeAndGetTier(schoolUpdateRequestDto.Type, out string tier))
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "Invalid school type");
            }

            DateTime timeNow = DateTime.UtcNow;
            school.NameFull = schoolUpdateRequestDto.NameFull;
            school.NameShort = schoolUpdateRequestDto.NameShort;
            school.State = schoolUpdateRequestDto.State;
            school.City = schoolUpdateRequestDto.City;
            school.Road = schoolUpdateRequestDto.Road;
            school.Building = schoolUpdateRequestDto.Building;
            school.Rspo = schoolUpdateRequestDto.Rspo;
            school.Type = schoolUpdateRequestDto.Type;
            school.Tier = tier;
            school.Email = schoolUpdateRequestDto.Email;
            school.UpdatedAt = timeNow;
            school.Version++;

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
                school.Tier,
                school.Email,
                school.CreatedAt,
                school.UpdatedAt,
                school.Version
            );
        }

        public async Task DeleteSchoolAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == id);

            if (school == null)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "School with this id does not exist");
            }

            _db.Schools.Remove(school);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSchoolsAsync(ClaimsPrincipal? claimsPrincipal, List<Guid> ids)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var schools = await _db.Schools.Where(s => ids.Contains(s.Id)).ToListAsync();

            if (schools.Count == 0)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "Not a single school with provided ids exists");
            }

            _db.Schools.RemoveRange(schools);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAllSchoolsAsync(ClaimsPrincipal? claimsPrincipal)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            await _db.Schools.ExecuteDeleteAsync();
        }

        public async Task<int> ImportSchoolsAsync(ClaimsPrincipal? claimsPrincipal, SchoolImportRequestDto schoolImportRequestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var file = schoolImportRequestDto.File;
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

            var expectedHeaders = new[] { "Numer RSPO", "Nazwa", "Województwo", "Miejscowość", "Ulica", "Numer budynku", "Typ", "E-mail" };
            var actualHeaders = csv.HeaderRecord;

            if (actualHeaders == null || !expectedHeaders.All(h => actualHeaders.Contains(h)))
            {
                throw new KoalaException(KoalaErrorCodes.FileReadFailure, "The uploaded CSV file is missing one or more required columns.");
            }

            while (await csv.ReadAsync())
            {
                var rawType = csv.GetField("Typ");

                if (string.IsNullOrWhiteSpace(rawType) || !SchoolValidateTypeAndGetTier(rawType, out var tier))
                {
                    continue;
                }

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
                    Type = rawType.Trim(),                       // Mapped to Type
                    Tier = tier,                                 // Custom Tier mapping (TIER_1 / TIER_2)
                    Email = csv.GetField("E-mail")!,             // Mapped to Email         
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

            if (queryResult == null)
            {
                throw new KoalaException(KoalaErrorCodes.SchoolNotFound, "Could not find a school with this id");
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
                queryResult.Tier,
                queryResult.Email,
                queryResult.CreatedAt,
                queryResult.UpdatedAt,
                queryResult.Version
            );
        }

        public async Task<(List<SchoolDto>, ApiPagination)> GetSchoolsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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
                    s.Tier,
                    s.Email,
                    s.CreatedAt,
                    s.UpdatedAt,
                    s.Version
                )).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize).ToList();

            return (schools, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }
    }
}