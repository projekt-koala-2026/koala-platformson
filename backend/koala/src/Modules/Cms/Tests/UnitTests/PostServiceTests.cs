using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class PostServiceTests
{
    public static IEnumerable<object[]> ForbiddenRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });
    public static IEnumerable<object[]> EditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> CreateEditionCases => EditionCases(isCreate: true);
    public static IEnumerable<object[]> UpdateEditionCases => EditionCases(isCreate: false);
    public static IEnumerable<object[]> DeleteAnyEditionCases =>
        from role in new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }
        from postId in new[] { TestDataSeeder.ID_POST_1, TestDataSeeder.ID_POST_2, TestDataSeeder.ID_POST_3 }
        select new object[] { role, postId };

    private static IEnumerable<object[]> EditionCases(bool isCreate)
    {
        foreach (var role in new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR })
        foreach (var state in new[] { LifecycleStates.STATE_ACTIVE, LifecycleStates.STATE_INACTIVE, LifecycleStates.STATE_CLOSED })
        foreach (var isVisible in new[] { false, true })
        {
            bool allowed = state == LifecycleStates.STATE_ACTIVE ||
                state == LifecycleStates.STATE_CLOSED && !isCreate ||
                state == LifecycleStates.STATE_INACTIVE && !isVisible;
            yield return [role, state, isVisible, allowed];
        }
    }

    private static CreatePostRequestDto CreateRequest(Guid? editionId = null, bool isVisible = true) => new(editionId ?? TestDataSeeder.EDITION_ID_1, "Created post", "{}", isVisible);
    private static UpdatePostRequestDto UpdateRequest(int version = 1, Guid? editionId = null, bool isVisible = true) => new(editionId ?? TestDataSeeder.EDITION_ID_1, "Updated post", "{}", isVisible, version);
    private static PostService CreateService(CmsDbContext db, Moq.Mock<koala.src.Shared.Core.ICoreModule>? core = null) =>
        new(db, (core ?? CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE)).Object);

    [Theory]
    [MemberData(nameof(CreateEditionCases))]
    public async Task CreatePost_AppliesEditionStateAndVisibilityRules(string role, string state, bool isVisible, bool allowed)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = state == LifecycleStates.STATE_ACTIVE ? TestDataSeeder.EDITION_ID_1 : TestDataSeeder.EDITION_ID_2;
        var core = CmsTestMocks.Edition(editionId, state);
        var action = () => CreateService(db, core).CreatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), CreateRequest(editionId, isVisible));

        if (allowed)
        {
            var result = await action();
            Assert.Equal(1, result.Version);
            Assert.Equal(isVisible, result.IsVisible);
            Assert.Equal("Created post", (await db.Posts.FindAsync(result.Id))!.Name);
            Assert.Equal(4, db.Posts.Count());
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(action);
            Assert.Equal(KoalaErrorCodes.EditionInvalidStates, error.ErrorCode);
            Assert.Equal(3, db.Posts.Count());
        }

        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Fact]
    public async Task CreatePost_RejectsUnauthenticatedPrincipal()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).CreatePostAsync(null, CreateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task CreatePost_RejectsEveryNonEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).CreatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), CreateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        Assert.Equal(3, db.Posts.Count());
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Fact]
    public async Task CreatePost_RejectsAuthenticatedUserWithoutRole()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).CreatePostAsync(CmsTestPrincipal.Authenticated(), CreateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Fact]
    public async Task CreatePost_RejectsUnknownEdition()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = Guid.NewGuid();
        var core = CmsTestMocks.Edition(editionId, null);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).CreatePostAsync(
            CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), CreateRequest(editionId)));
        Assert.Equal(KoalaErrorCodes.EditionNotFound, error.ErrorCode);
        Assert.Equal(3, db.Posts.Count());
        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task CreatePost_RejectsUnknownEditionForEachEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = Guid.NewGuid();
        var core = CmsTestMocks.Edition(editionId, null);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).CreatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), CreateRequest(editionId)));
        Assert.Equal(KoalaErrorCodes.EditionNotFound, error.ErrorCode);
        Assert.Equal(3, db.Posts.Count());
        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Fact]
    public async Task UpdatePost_RejectsUnauthenticatedPrincipal()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(null, TestDataSeeder.ID_POST_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task UpdatePost_RejectsEveryNonEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Fact]
    public async Task UpdatePost_RejectsAuthenticatedUserWithoutRole()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(), TestDataSeeder.ID_POST_1, UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdatePost_MissingPostReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid(), UpdateRequest()));
        Assert.Equal(KoalaErrorCodes.PostNotFound, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdatePost_StaleVersionReturnsConflict(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1, UpdateRequest(99)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Fact]
    public async Task UpdatePost_RejectsUnknownEditionWithoutChangingPost()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = Guid.NewGuid();
        var core = CmsTestMocks.Edition(editionId, null);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(
            CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), TestDataSeeder.ID_POST_1, UpdateRequest(editionId: editionId)));
        Assert.Equal(KoalaErrorCodes.EditionNotFound, error.ErrorCode);
        Assert.Equal(TestDataSeeder.EDITION_ID_1, (await db.Posts.FindAsync(TestDataSeeder.ID_POST_1))!.EditionId);
        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdatePost_RejectsUnknownEditionForEachEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = Guid.NewGuid();
        var core = CmsTestMocks.Edition(editionId, null);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1, UpdateRequest(editionId: editionId)));
        Assert.Equal(KoalaErrorCodes.EditionNotFound, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Theory]
    [MemberData(nameof(UpdateEditionCases))]
    public async Task UpdatePost_AppliesEditionStateAndVisibilityRules(string role, string state, bool isVisible, bool allowed)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var editionId = state == LifecycleStates.STATE_ACTIVE ? TestDataSeeder.EDITION_ID_1 : TestDataSeeder.EDITION_ID_2;
        var postId = state == LifecycleStates.STATE_CLOSED ? TestDataSeeder.ID_POST_3 : TestDataSeeder.ID_POST_1;
        var core = CmsTestMocks.Edition(editionId, state);
        var action = () => CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), postId, UpdateRequest(editionId: editionId, isVisible: isVisible));

        if (allowed)
        {
            var result = await action();
            Assert.Equal(2, result.Version);
            Assert.Equal(editionId, result.EditionId);
            Assert.Equal(isVisible, result.IsVisible);
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(action);
            Assert.Equal(KoalaErrorCodes.EditionInvalidStates, error.ErrorCode);
            var unchangedPost = (await db.Posts.FindAsync(postId))!;
            var originalEditionId = postId == TestDataSeeder.ID_POST_1
                ? TestDataSeeder.EDITION_ID_1
                : TestDataSeeder.EDITION_ID_2;
            Assert.Equal(originalEditionId, unchangedPost.EditionId);
            Assert.Equal(1, unchangedPost.Version);
        }

        core.Verify(module => module.Internal_GetEditionAsync(editionId), Moq.Times.Once);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdatePost_CannotMovePostIntoClosedEdition(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_2, LifecycleStates.STATE_CLOSED);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).UpdatePostAsync(
            CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1,
            UpdateRequest(editionId: TestDataSeeder.EDITION_ID_2, isVisible: false)));

        Assert.Equal(KoalaErrorCodes.EditionInvalidStates, error.ErrorCode);
        var post = (await db.Posts.FindAsync(TestDataSeeder.ID_POST_1))!;
        Assert.Equal(TestDataSeeder.EDITION_ID_1, post.EditionId);
        Assert.Equal(1, post.Version);
        core.Verify(module => module.Internal_GetEditionAsync(TestDataSeeder.EDITION_ID_2), Moq.Times.Once);
    }

    [Fact]
    public async Task DeletePost_RejectsUnauthenticatedPrincipal()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).DeletePostAsync(null, TestDataSeeder.ID_POST_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task DeletePost_RejectsEveryNonEditorRole(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).DeletePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Fact]
    public async Task DeletePost_RejectsAuthenticatedUserWithoutRole()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db, core).DeletePostAsync(CmsTestPrincipal.Authenticated(), TestDataSeeder.ID_POST_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task DeletePost_MissingPostReturnsNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db).DeletePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.PostNotFound, error.ErrorCode);
    }

    [Theory]
    [InlineData(null, false, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, false, 1)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, false, 1)]
    [InlineData("", false, 1)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, true, 2)]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, true, 2)]
    public async Task GetPosts_HiddenVisibilityDependsOnEditorRole(string? role, bool showHidden, int expectedCount)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = role is null ? null : role.Length == 0 ? CmsTestPrincipal.Authenticated() : CmsTestPrincipal.Authenticated(roles: [role]);
        var (posts, pagination) = await CreateService(db).GetPostsAsync(principal, new PageQueryDto(0, 10), new PostQueryDto(null, null, showHidden));
        Assert.Equal(expectedCount, posts.Count);
        Assert.Equal(expectedCount, pagination.TotalNumber);
    }

    [Fact]
    public async Task GetPosts_NonEditorCannotRequestHiddenPosts()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db).GetPostsAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_REVIEWER]), new PageQueryDto(0, 10), new PostQueryDto(null, null, true)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN)]
    public async Task GetPosts_EveryNonEditorRoleCannotRequestHiddenPosts(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db).GetPostsAsync(CmsTestPrincipal.Authenticated(roles: [role]), new PageQueryDto(0, 10), new PostQueryDto(null, null, true)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task GetPosts_AuthenticatedUserWithoutRolesCannotRequestHiddenPosts()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db).GetPostsAsync(CmsTestPrincipal.Authenticated(), new PageQueryDto(0, 10), new PostQueryDto(null, null, true)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task GetPosts_FiltersByEditionAndNameAndPaginates()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var (page, pagination) = await CreateService(db).GetPostsAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), new PageQueryDto(0, 1), new PostQueryDto(TestDataSeeder.EDITION_ID_1, TestDataSeeder.NAME_POST_1, null));
        Assert.Single(page);
        Assert.Equal(TestDataSeeder.ID_POST_1, page[0].Id);
        Assert.Equal(1, pagination.TotalNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN)]
    public async Task GetPosts_ShowHiddenFalseReturnsOnlyVisiblePosts(string? role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = role is null ? null : role.Length == 0 ? CmsTestPrincipal.Authenticated() : CmsTestPrincipal.Authenticated(roles: [role]);
        var (items, pagination) = await CreateService(db).GetPostsAsync(principal, new PageQueryDto(0, 10), new PostQueryDto(null, null, false));
        Assert.Single(items);
        Assert.True(items[0].IsVisible);
        Assert.Equal(1, pagination.TotalNumber);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task UpdatePost_ExistingVersionUpdatesFieldsAndIncrementsVersion(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        var result = await CreateService(db, core).UpdatePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_POST_1, UpdateRequest());
        Assert.Equal(2, result.Version);
        Assert.Equal("Updated post", result.Name);
        Assert.True(result.IsVisible);
        core.Verify(module => module.Internal_GetEditionAsync(TestDataSeeder.EDITION_ID_1), Moq.Times.Once);
    }

    [Theory]
    [MemberData(nameof(DeleteAnyEditionCases))]
    public async Task DeletePost_DeletesPostsForAnyEditionWithoutCallingCore(string role, Guid postId)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var core = CmsTestMocks.Edition(TestDataSeeder.EDITION_ID_1, LifecycleStates.STATE_ACTIVE);
        await CreateService(db, core).DeletePostAsync(CmsTestPrincipal.Authenticated(roles: [role]), postId);
        Assert.Null(await db.Posts.FindAsync(postId));
        core.Verify(module => module.Internal_GetEditionAsync(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR, true)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER, false)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN, false)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN, false)]
    public async Task GetPost_HiddenPostAvailableOnlyToEditorRoles(string? role, bool canSeeHidden)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var service = CreateService(db);
        var principal = role is null ? null : role.Length == 0 ? CmsTestPrincipal.Authenticated() : CmsTestPrincipal.Authenticated(roles: [role]);
        if (canSeeHidden)
        {
            Assert.Equal(TestDataSeeder.ID_POST_2, (await service.GetPostAsync(principal, TestDataSeeder.ID_POST_2)).Id);
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => service.GetPostAsync(principal, TestDataSeeder.ID_POST_2));
            Assert.Equal(KoalaErrorCodes.PostNotFound, error.ErrorCode);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(RoleNames.NAME_ORGANIZATION_ADMIN)]
    [InlineData(RoleNames.NAME_ORGANIZATION_EDITOR)]
    [InlineData(RoleNames.NAME_ORGANIZATION_REVIEWER)]
    [InlineData(RoleNames.NAME_TEAM_CAPTAIN)]
    [InlineData(RoleNames.NAME_TEAM_GUARDIAN)]
    public async Task GetPost_VisiblePostAvailableToEveryPrincipal(string? role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var principal = role is null ? null : role.Length == 0 ? CmsTestPrincipal.Authenticated() : CmsTestPrincipal.Authenticated(roles: [role]);
        var result = await CreateService(db).GetPostAsync(principal, TestDataSeeder.ID_POST_1);
        Assert.Equal(TestDataSeeder.ID_POST_1, result.Id);
        Assert.True(result.IsVisible);
    }

    [Fact]
    public async Task GetPost_MissingPostReturnsNotFound()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => CreateService(db).GetPostAsync(null, Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.PostNotFound, error.ErrorCode);
    }
}
