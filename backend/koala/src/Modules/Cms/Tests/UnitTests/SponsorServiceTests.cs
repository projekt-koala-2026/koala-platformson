using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class SponsorServiceTests
{
    public static IEnumerable<object[]> EditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> ForbiddenRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });
    private static CreateSponsorRequestDto CreateRequest() => new("New sponsor", "{}", true);
    private static UpdateSponsorRequestDto UpdateRequest(int version = 1) => new("Changed sponsor", "{}", true, version);

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task CreateSponsor_AllowsAdminAndEditor_AndStartsAtVersionOne(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await new SponsorService(db).CreateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), CreateRequest());
        Assert.Equal(1, result.Version);
        Assert.True(result.IsVisible);
    }

    [Fact]
    public async Task CreateSponsor_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).CreateSponsorAsync(null, CreateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task CreateSponsor_RejectsNonEditorRoles(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).CreateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), CreateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateSponsor_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).UpdateSponsorAsync(null, TestDataSeeder.ID_SPONSOR_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task UpdateSponsor_RejectsNonEditorRoles(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).UpdateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateSponsor_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).UpdateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid(), UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.SponsorNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateSponsor_StaleVersionReturnsConflict(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).UpdateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_1, UpdateRequest(99)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateSponsor_ExistingVersionUpdatesFieldsAndIncrementsVersion(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await new SponsorService(db).UpdateSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Changed sponsor", result.Name);
        Assert.True(result.IsVisible);
    }

    [Fact]
    public async Task DeleteSponsor_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).DeleteSponsorAsync(null, TestDataSeeder.ID_SPONSOR_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task DeleteSponsor_RejectsNonEditorRoles(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).DeleteSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task DeleteSponsor_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).DeleteSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.SponsorNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task DeleteSponsor_ExistingSponsorDeletesRow(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        await new SponsorService(db).DeleteSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_1);
        Assert.Null(await db.Sponsors.FindAsync(TestDataSeeder.ID_SPONSOR_1));
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, 1)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, 1)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, 2)]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, 2)]
    public async Task GetSponsors_HidesUnpublishedFromNonEditors(string? role, int expected)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = role is null ? null : CmsTestPrincipal.Authenticated(roles: [role]);
        var (items, pagination) = await new SponsorService(db).GetSponsorsAsync(principal, new PageQueryDto(0, 10));
        Assert.Equal(expected, items.Count);
        Assert.Equal(expected, pagination.TotalNumber);
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, false)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, false)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN, false)]
    public async Task GetSponsor_HiddenRecordVisibilityFollowsRole(string role, bool canSee)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var service = new SponsorService(db);
        var action = () => service.GetSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_SPONSOR_2);
        if (canSee)
            Assert.Equal(TestDataSeeder.NAME_SPONSOR_2, (await action()).Name);
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(action);
            Assert.Equal(KoalaErrorCodes.SponsorNotFound, error.ErrorCode);
        }
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR)]
    public async Task GetSponsor_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new SponsorService(db).GetSponsorAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.SponsorNotFound, error.ErrorCode);
    }
}
