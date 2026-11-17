using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace koala.src.Modules.Cms.Tests.UnitTests;

/// <summary>
/// Seeds CMS-specific test rows. Static pages are already seeded by CmsDbContext.OnModelCreating.
/// </summary>
public static class TestDataSeeder
{
    public static readonly Guid EDITION_ID_1 = Guid.CreateVersion7();
    public static readonly Guid EDITION_ID_2 = Guid.CreateVersion7();
    public static readonly Guid USER_ID_1 = Guid.CreateVersion7();
    public static readonly Guid USER_ID_2 = Guid.CreateVersion7();
    public static readonly Guid ID_POST_1 = Guid.CreateVersion7();
    public static readonly Guid ID_POST_2 = Guid.CreateVersion7();
    public static readonly Guid ID_POST_3 = Guid.CreateVersion7();
    public static readonly Guid ID_SPONSOR_1 = Guid.CreateVersion7();
    public static readonly Guid ID_SPONSOR_2 = Guid.CreateVersion7();
    public static readonly Guid ID_PUBLIC_FILE_1 = Guid.CreateVersion7();
    public static readonly Guid ID_PUBLIC_FILE_2 = Guid.CreateVersion7();

    public const string NAME_POST_1 = "Visible test post";
    public const string NAME_POST_2 = "Hidden test post";
    public const string NAME_POST_3 = "Closed edition test post";
    public const string NAME_SPONSOR_1 = "Visible test sponsor";
    public const string NAME_SPONSOR_2 = "Hidden test sponsor";
    public const string NAME_PUBLIC_FILE_1 = "Test image";
    public const string NAME_PUBLIC_FILE_2 = "Test document";
    public const string PATH_PUBLIC_FILE_1 = "test-image.png";
    public const string PATH_PUBLIC_FILE_2 = "test-document.pdf";
    public const string NAME_FIRST_KOALICJANT_1 = "Alicja";
    public const string NAME_LAST_KOALICJANT_1 = "Testowa";
    public const string EMAIL_KOALICJANT_1 = "alicja.testowa@example.test";
    public const string NAME_FIRST_KOALICJANT_2 = "Jan";
    public const string NAME_LAST_KOALICJANT_2 = "Ukryty";
    public const string EMAIL_KOALICJANT_2 = "jan.ukryty@example.test";
    public const int VERSION = 1;
    public static readonly DateTime TIME_CREATED = DateTime.UtcNow;
    public static readonly DateTime TIME_UPDATED = DateTime.UtcNow;

    public static async Task<CmsDbContext> SeedDb(CmsDbContext db)
    {
        var posts = new List<Post>
        {
            new Post
            {
                Id = ID_POST_1,
                EditionId = EDITION_ID_1,
                Name = NAME_POST_1,
                ContentJson = "{}",
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                IsVisible = true,
                Version = VERSION
            },
            new Post
            {
                Id = ID_POST_2,
                EditionId = EDITION_ID_1,
                Name = NAME_POST_2,
                ContentJson = "{}",
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                IsVisible = false,
                Version = VERSION
            },
            new Post
            {
                Id = ID_POST_3,
                EditionId = EDITION_ID_2,
                Name = NAME_POST_3,
                ContentJson = "{}",
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                IsVisible = false,
                Version = VERSION
            }
        };

        var sponsors = new List<Sponsor>
        {
            new Sponsor
            {
                Id = ID_SPONSOR_1,
                Name = NAME_SPONSOR_1,
                ContentJson = "{}",
                IsVisible = true,
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                Version = VERSION
            },
            new Sponsor
            {
                Id = ID_SPONSOR_2,
                Name = NAME_SPONSOR_2,
                ContentJson = "{}",
                IsVisible = false,
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                Version = VERSION
            }
        };

        var publicFiles = new List<PublicFile>
        {
            new PublicFile
            {
                Id = ID_PUBLIC_FILE_1,
                Name = NAME_PUBLIC_FILE_1,
                Path = PATH_PUBLIC_FILE_1,
                Type = "image/png",
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                Version = VERSION
            },
            new PublicFile
            {
                Id = ID_PUBLIC_FILE_2,
                Name = NAME_PUBLIC_FILE_2,
                Path = PATH_PUBLIC_FILE_2,
                Type = "application/pdf",
                CreatedAt = TIME_CREATED,
                UpdatedAt = TIME_UPDATED,
                Version = VERSION
            }
        };

        var koalicjants = new List<Koalicjant>
        {
            new Koalicjant
            {
                Id = USER_ID_1,
                NameFirst = NAME_FIRST_KOALICJANT_1,
                NameLast = NAME_LAST_KOALICJANT_1,
                Email = EMAIL_KOALICJANT_1,
                ContentJson = "{}",
                UpdatedAt = TIME_UPDATED,
                Version = VERSION,
                IsVisible = true
            },
            new Koalicjant
            {
                Id = USER_ID_2,
                NameFirst = NAME_FIRST_KOALICJANT_2,
                NameLast = NAME_LAST_KOALICJANT_2,
                Email = EMAIL_KOALICJANT_2,
                ContentJson = "{}",
                UpdatedAt = TIME_UPDATED,
                Version = VERSION,
                IsVisible = false
            }
        };

        await db.Posts.AddRangeAsync(posts);
        await db.Sponsors.AddRangeAsync(sponsors);
        await db.PublicFiles.AddRangeAsync(publicFiles);
        await db.Koalicjants.AddRangeAsync(koalicjants);
        await db.SaveChangesAsync();
        return db;
    }
}

public static class CmsTestRoles
{
    public static readonly string[] OrganizationRoles =
    [
        RoleNames.NAME_ORGANIZATION_ADMIN,
        RoleNames.NAME_ORGANIZATION_EDITOR,
        RoleNames.NAME_ORGANIZATION_REVIEWER
    ];

    public static readonly string[] TeamRoles =
    [
        RoleNames.NAME_TEAM_CAPTAIN,
        RoleNames.NAME_TEAM_GUARDIAN
    ];

    public static readonly string[] NonEditorRoles = [OrganizationRoles[2], .. TeamRoles];
}

public static class CmsTestPrincipal
{
    public static ClaimsPrincipal Authenticated(Guid? userId = null, params string[] roles)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "CmsTest"));
    }

    public static ClaimsPrincipal Unauthenticated() => new(new ClaimsIdentity());
}

public static class CmsTestDatabase
{
    public static async Task<CmsDbContext> CreateSeededAsync()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CmsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedDb(db);
        return db;
    }
}

public static class CmsTestMocks
{
    public static Mock<ICoreModule> Edition(Guid editionId, string? state)
    {
        var coreModule = new Mock<ICoreModule>(MockBehavior.Strict);
        var setup = coreModule.Setup(module => module.Internal_GetEditionAsync(editionId));

        if (state == null)
        {
            setup.ReturnsAsync((_EditionDto?)null);
        }
        else
        {
            setup.ReturnsAsync(new _EditionDto(
                editionId,
                "Mocked edition",
                state,
                TestDataSeeder.TIME_CREATED,
                TestDataSeeder.TIME_CREATED.AddDays(1),
                TestDataSeeder.TIME_CREATED,
                TestDataSeeder.TIME_UPDATED,
                TestDataSeeder.VERSION));
        }

        return coreModule;
    }
}
