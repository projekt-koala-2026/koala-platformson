using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class KoalicjantServiceTests
{
    public static IEnumerable<object[]> OrganizationEditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> NonEditorRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });
    private static UpdateKoalicjantRequestDto UpdateRequest(int version = 1) => new("Updated", "Name", "updated@example.test", "{}", true, version);

    [Theory]
    [InlineData(null, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, 1)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, 1)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, 2)]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, 2)]
    public async Task GetKoalicjants_HidesUnpublishedRecordsFromNonEditors(string? role, int expected)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = role is null ? null : CmsTestPrincipal.Authenticated(roles: [role]);
        var (items, pagination) = await new KoalicjantService(db).GetKoalicjantsAsync(principal, new PageQueryDto(0, 10));
        Assert.Equal(expected, items.Count);
        Assert.Equal(expected, pagination.TotalNumber);
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, false)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, false)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN, false)]
    public async Task GetKoalicjant_HiddenRecordIsRestricted(string role, bool canSee)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var service = new KoalicjantService(db);
        var action = () => service.GetKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_KOALICJANT_2);
        if (canSee)
            Assert.Equal(TestDataSeeder.ID_KOALICJANT_2, (await action()).Id);
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(action);
            Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
        }
    }

    [Fact]
    public async Task GetKoalicjant_MissingIdReturnsNotFound()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).GetKoalicjantAsync(null, Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR)]
    public async Task GetKoalicjant_MissingIdReturnsNotFoundForEditors(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).GetKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).UpdateKoalicjantAsync(null, TestDataSeeder.ID_KOALICJANT_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(NonEditorRoles))]
    public async Task UpdateKoalicjant_RejectsOtherUsersWithoutEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2, role);
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).UpdateKoalicjantAsync(principal, TestDataSeeder.ID_KOALICJANT_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_RejectsAuthenticatedUserWithoutRoleWhenNotOwner()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2), TestDataSeeder.ID_KOALICJANT_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_AllowsOwnerWithoutAdministrativeRole()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await new KoalicjantService(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_1), TestDataSeeder.ID_KOALICJANT_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated", result.NameFirst);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid(), UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_StaleVersionReturnsConflict(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_KOALICJANT_1, UpdateRequest(20)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_AllowsAdminAndEditor(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await new KoalicjantService(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_KOALICJANT_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated", result.NameFirst);
    }
}
