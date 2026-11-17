using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using koala.src.Shared.Account;
using Moq;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class KoalicjantServiceTests
{
    public static IEnumerable<object[]> OrganizationEditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> NonEditorRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });
    public static IEnumerable<object[]> AddForbiddenRoles => new[]
    {
        RoleNames.NAME_ORGANIZATION_REVIEWER,
        RoleNames.NAME_TEAM_CAPTAIN,
        RoleNames.NAME_TEAM_GUARDIAN
    }.Select(role => new object[] { role });
    private static UpsertKoalicjantRequestDto UpdateRequest(int version = 1) => new("Updated", "Name", "updated@example.test", "{}", true, version);

    private static Mock<IAccountModule> AccountModule(bool organizationUser = true)
    {
        var account = new Mock<IAccountModule>(MockBehavior.Strict);
        account.Setup(module => module.Internal_IsOrganizationUser(It.IsAny<Guid>())).ReturnsAsync(organizationUser);
        return account;
    }

    private static KoalicjantService Service(CmsDbContext db) => new(db, AccountModule().Object);

    [Fact]
    public async Task AddKoalicjant_CreatesProfileForSelfAndChecksAccountModuleOnce()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule();
        var id = Guid.NewGuid();
        var result = await new KoalicjantService(db, account.Object).AddKoalicjantAsync(
            CmsTestPrincipal.Authenticated(id), id, UpdateRequest());

        Assert.Equal(id, result.Id);
        Assert.Equal("Updated", result.NameFirst);
        Assert.Equal(1, result.Version);
        db.ChangeTracker.Clear();
        var persisted = await db.Koalicjants.FindAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal("Updated", persisted.NameFirst);
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        account.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddKoalicjant_AnonymousIsRejectedBeforeAccountModuleCall()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = new Mock<IAccountModule>(MockBehavior.Strict);
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .AddKoalicjantAsync(null, Guid.NewGuid(), UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        account.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddKoalicjant_InvalidOrganizationUserReturnsNotFound()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule(false);
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .AddKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_REVIEWER]), id, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        account.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddKoalicjant_ExistingProfileReturnsAlreadyExists()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule();
        var id = TestDataSeeder.USER_ID_1;
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .AddKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), id, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.KoalicjantAlreadyExists, error.ErrorCode);
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        account.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddKoalicjant_ReviewerCannotCreateAnotherUsersProfile()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule();
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .AddKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2, RoleNames.NAME_ORGANIZATION_REVIEWER), id, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        Assert.DoesNotContain(db.Koalicjants.Local, item => item.Id == id);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task AddKoalicjant_AdminAndEditorCanCreateAnotherOrganizationUsersProfile(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule();
        var id = Guid.NewGuid();

        var result = await new KoalicjantService(db, account.Object).AddKoalicjantAsync(
            CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2, role), id, UpdateRequest());

        Assert.Equal(id, result.Id);
        Assert.Equal(1, result.Version);
        Assert.NotNull(await db.Koalicjants.FindAsync(id));
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        account.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AddForbiddenRoles))]
    public async Task AddKoalicjant_NonAdminEditorCannotCreateAnotherUsersProfile(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule();
        var id = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .AddKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2, role), id, UpdateRequest()));

        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        Assert.Null(await db.Koalicjants.FindAsync(id));
        account.Verify(module => module.Internal_IsOrganizationUser(id), Times.Once);
        account.VerifyNoOtherCalls();
    }

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
        var (items, pagination) = await Service(db).GetKoalicjantsAsync(principal, new PageQueryDto(0, 10));
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
        var service = Service(db);
        var action = () => service.GetKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.USER_ID_2);
        if (canSee)
            Assert.Equal(TestDataSeeder.USER_ID_2, (await action()).Id);
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
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).GetKoalicjantAsync(null, Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR)]
    public async Task GetKoalicjant_MissingIdReturnsNotFoundForEditors(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).GetKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).UpdateKoalicjantAsync(null, TestDataSeeder.USER_ID_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(NonEditorRoles))]
    public async Task UpdateKoalicjant_RejectsOtherUsersWithoutEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2, role);
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).UpdateKoalicjantAsync(principal, TestDataSeeder.USER_ID_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_RejectsAuthenticatedUserWithoutRoleWhenNotOwner()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_2), TestDataSeeder.USER_ID_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task UpdateKoalicjant_RejectsIdNotRecognizedAsOrganizationUser()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var account = AccountModule(false);
        var error = await Assert.ThrowsAsync<KoalaException>(() => new KoalicjantService(db, account.Object)
            .UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), TestDataSeeder.USER_ID_1, UpdateRequest()));

        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
        account.Verify(module => module.Internal_IsOrganizationUser(TestDataSeeder.USER_ID_1), Times.Once);
        account.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKoalicjant_AllowsOwnerWithoutAdministrativeRole()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await Service(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(TestDataSeeder.USER_ID_1), TestDataSeeder.USER_ID_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated", result.NameFirst);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid(), UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.KoalicjantNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_StaleVersionReturnsConflict(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.USER_ID_1, UpdateRequest(20)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(OrganizationEditorRoles))]
    public async Task UpdateKoalicjant_AllowsAdminAndEditor(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await Service(db).UpdateKoalicjantAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.USER_ID_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated", result.NameFirst);
    }
}
