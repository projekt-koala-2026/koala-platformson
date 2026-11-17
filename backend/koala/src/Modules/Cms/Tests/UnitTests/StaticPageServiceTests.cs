using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class StaticPageServiceTests
{
    public static IEnumerable<object[]> EditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> ForbiddenRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });

    [Fact]
    public async Task GetStaticPages_ReturnsAllModelSeededPagesForAnonymousCaller()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var pages = await new StaticPageService(db).GetStaticPagesAsync(null);
        Assert.Equal(5, pages.Count);
        Assert.All(pages, page => Assert.Equal(1, page.Version));
    }

    [Fact]
    public async Task UpdateStaticPage_RejectsAnonymous()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new StaticPageService(db).UpdateStaticPagesAsync(null, Guid.Parse(CmsDbContext.static_id_guid_page_home), new UpdateStaticPageRequestDto("{}", 1)));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task UpdateStaticPage_RejectsNonEditorRoles(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new StaticPageService(db).UpdateStaticPagesAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.Parse(CmsDbContext.static_id_guid_page_home), new UpdateStaticPageRequestDto("{}", 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateStaticPage_InvalidJsonReturnsInvalidJsonError(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new StaticPageService(db).UpdateStaticPagesAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.Parse(CmsDbContext.static_id_guid_page_home), new UpdateStaticPageRequestDto("not json", 1)));
        Assert.Equal(KoalaErrorCodes.InvalidJson, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateStaticPage_MissingIdReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new StaticPageService(db).UpdateStaticPagesAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid(), new UpdateStaticPageRequestDto("{}", 1)));
        Assert.Equal(KoalaErrorCodes.StaticPageNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateStaticPage_StaleVersionReturnsConflict(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new StaticPageService(db).UpdateStaticPagesAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.Parse(CmsDbContext.static_id_guid_page_home), new UpdateStaticPageRequestDto("{}", 50)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdateStaticPage_ValidJsonUpdatesAndIncrementsVersion(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var result = await new StaticPageService(db).UpdateStaticPagesAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.Parse(CmsDbContext.static_id_guid_page_home), new UpdateStaticPageRequestDto("{\"body\":\"new\"}", 1));
        Assert.Equal(2, result.Version);
        Assert.Equal("{\"body\":\"new\"}", result.ContentJson);
    }
}
