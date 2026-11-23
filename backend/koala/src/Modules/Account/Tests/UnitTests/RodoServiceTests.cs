using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace koala.src.Modules.Account.Tests.UnitTests;

public class RodoServiceTests
{
    private const string FilesPath = "rodo-test-files";
    private static readonly string[] AllRoles =
    [
        RoleNames.NAME_ORGANIZATION_ADMIN,
        RoleNames.NAME_ORGANIZATION_RODO_MODERATOR,
        RoleNames.NAME_ORGANIZATION_EDITOR,
        RoleNames.NAME_ORGANIZATION_REVIEWER,
        RoleNames.NAME_TEAM_GUARDIAN,
        RoleNames.NAME_TEAM_CAPTAIN
    ];

    private sealed class TestAccountDbContext(DbContextOptions<AccountDbContext> options) : AccountDbContext(options)
    {
        public bool FailSaveChanges { get; set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            FailSaveChanges ? Task.FromException<int>(new InvalidOperationException("simulated save failure")) : base.SaveChangesAsync(cancellationToken);
    }

    private static ClaimsPrincipal Principal(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));
    }

    private static ClaimsPrincipal TeamGuardian() => Principal(TestDataSeeder.ID_USER_8, RoleNames.NAME_TEAM_GUARDIAN);
    private static ClaimsPrincipal OrganizationAdmin() => Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_RODO_MODERATOR);
    private static ClaimsPrincipal NonMemberGuardian() => Principal(TestDataSeeder.ID_USER_9, RoleNames.NAME_TEAM_GUARDIAN);

    public static IEnumerable<object[]> AllRoleCases => AllRoles.Select(role => new object[] { role });
    public static IEnumerable<object[]> NonGuardianRoleCases => AllRoles.Where(role => role != RoleNames.NAME_TEAM_GUARDIAN).Select(role => new object[] { role });
    public static IEnumerable<object[]> NonOrganizationAdminRoleCases => AllRoles.Where(role => role != RoleNames.NAME_ORGANIZATION_RODO_MODERATOR).Select(role => new object[] { role });
    public static IEnumerable<object[]> SeededReadRoleCases => new object[][]
    {
        [TestDataSeeder.ID_USER_1, new[] { RoleNames.NAME_ORGANIZATION_RODO_MODERATOR }, true],
        [TestDataSeeder.ID_USER_2, new[] { RoleNames.NAME_ORGANIZATION_ADMIN }, false],
        [TestDataSeeder.ID_USER_3, new[] { RoleNames.NAME_ORGANIZATION_EDITOR, RoleNames.NAME_ORGANIZATION_REVIEWER }, false],
        [TestDataSeeder.ID_USER_4, new[] { RoleNames.NAME_ORGANIZATION_EDITOR }, false],
        [TestDataSeeder.ID_USER_5, new[] { RoleNames.NAME_ORGANIZATION_EDITOR }, false],
        [TestDataSeeder.ID_USER_6, new[] { RoleNames.NAME_ORGANIZATION_REVIEWER }, false],
        [TestDataSeeder.ID_USER_7, new[] { RoleNames.NAME_ORGANIZATION_REVIEWER }, false],
        [TestDataSeeder.ID_USER_8, new[] { RoleNames.NAME_TEAM_GUARDIAN }, true],
        [TestDataSeeder.ID_USER_9, new[] { RoleNames.NAME_TEAM_GUARDIAN }, false],
        [TestDataSeeder.ID_USER_10, new[] { RoleNames.NAME_TEAM_CAPTAIN }, false],
        [TestDataSeeder.ID_USER_11, new[] { RoleNames.NAME_TEAM_CAPTAIN }, false],
        [TestDataSeeder.ID_USER_12, new[] { RoleNames.NAME_TEAM_GUARDIAN, RoleNames.NAME_TEAM_CAPTAIN }, false]
    };
    public static IEnumerable<object[]> NonOwnerGuardianUsers => new object[][]
    {
        [TestDataSeeder.ID_USER_9]
    };
    public static IEnumerable<object[]> AllowedRodoUploads => new (string Extension, string ContentType)[]
    {
        (".jpg", "image/jpeg"), (".jpeg", "image/jpeg"), (".png", "image/png"),
        (".webp", "image/webp"), (".pdf", "application/pdf")
    }.Select(x => new object[] { x.Extension, x.ContentType });

    private static Mock<IRodoFileOperations> FileOperations()
    {
        var files = new Mock<IRodoFileOperations>(MockBehavior.Strict);
        files.Setup(file => file.CreateDirectory(It.IsAny<string>()));
        files.Setup(file => file.Move(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
        files.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        files.Setup(file => file.ReadAllBytesAsync(It.IsAny<string>())).ReturnsAsync("saved rodo"u8.ToArray());
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);
        files.Setup(file => file.Delete(It.IsAny<string>()));
        return files;
    }

    private static async Task<TestAccountDbContext> SeededDb()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new TestAccountDbContext(options);
        await TestDataSeeder.SeedDb(db);
        return db;
    }

    private static RodoService Service(AccountDbContext db, Mock<IRodoFileOperations> files) => new(db, FilesPath, files.Object);

    private static RodoUpsertDto UploadDto(string fileName = "consent.pdf", int? version = null, string contentType = "application/pdf")
    {
        var file = new Mock<IFormFile>(MockBehavior.Strict);
        file.SetupGet(value => value.FileName).Returns(fileName);
        file.SetupGet(value => value.ContentType).Returns(contentType);
        return new RodoUpsertDto(file.Object, version);
    }

    private static async Task<Rodo> AddRodoAsync(AccountDbContext db, Guid teamMemberId, string type = ".pdf", int version = 1)
    {
        var member = await db.TeamMembers.FirstAsync(item => item.Id == teamMemberId);
        var rodo = new Rodo
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            CreatedAt = TestDataSeeder.TIME_CREATED,
            UpdatedAt = TestDataSeeder.TIME_CREATED,
            Version = version,
            TeamMember = member
        };
        member.RodoId = rodo.Id;
        member.Rodo = rodo;
        member.RodoState = TeamMemberRodoState.RODOSTATE_ACCEPETED;
        db.Rodos.Add(rodo);
        await db.SaveChangesAsync();
        return rodo;
    }

    [Fact]
    public async Task AddRodo_SavesDocumentAndStartsAtVersionOne()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var result = await Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto());

        Assert.Equal(TestDataSeeder.ID_TEAM_MEMBER_1, result.TeamMemberId);
        Assert.Equal(TestDataSeeder.ID_TEAM_1, result.TeamId);
        Assert.Equal(TeamMemberRodoState.RODOSTATE_UNVERIFIED, result.RodoState);
        Assert.Equal(1, result.RodoVersion);
        Assert.Equal(1, db.Rodos.Count());
        var member = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        Assert.NotNull(member!.RodoId);
        Assert.Equal(TeamMemberRodoState.RODOSTATE_UNVERIFIED, member.RodoState);
        files.Verify(file => file.CreateDirectory(FilesPath), Times.Once);
        files.Verify(file => file.WriteAsync(It.Is<string>(path => path.EndsWith(".pdf")), It.IsAny<IFormFile>(), false), Times.Once);
        files.Verify(file => file.ReadAllBytesAsync(It.IsAny<string>()), Times.Once);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(AllowedRodoUploads))]
    public async Task AddRodo_AcceptsEveryConfiguredFileExtension(string extension, string contentType)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var dto = UploadDto($"consent{extension}", contentType: contentType);

        var result = await Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, dto);

        Assert.Equal(extension, (await db.Rodos.SingleAsync()).Type);
        files.Verify(file => file.WriteAsync(It.Is<string>(path => path.EndsWith(extension)), It.IsAny<IFormFile>(), false), Times.Once);
    }

    [Fact]
    public async Task AddRodo_AnonymousUserIsRejectedBeforeFileOperations()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonGuardianRoleCases))]
    public async Task AddRodo_NonGuardianRolesAreRejected(string role)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(Principal(TestDataSeeder.ID_USER_1, role), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddRodo_AuthenticatedUserWithoutRoleIsRejected()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(Principal(TestDataSeeder.ID_USER_8), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddRodo_GuardianWhoDoesNotOwnTheTeamIsForbidden()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddRodo_GuardianCannotAddToAnotherTeam()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_2, TestDataSeeder.ID_TEAM_MEMBER_3, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("missing-member")]
    [InlineData("member-in-other-team")]
    public async Task AddRodo_MissingOrMismatchedMemberReturnsAlreadyExists(string target)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var memberId = target == "missing-member" ? Guid.NewGuid() : TestDataSeeder.ID_TEAM_MEMBER_3;
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, memberId, UploadDto()));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoAlreadyExists, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddRodo_ExistingRodoReturnsAlreadyExists()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoAlreadyExists, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddRodo_DirectoryFailureReturnsFolderCreationFailure()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        files.Setup(file => file.CreateDirectory(FilesPath)).Throws(new IOException("directory failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.FolderCreationFailure, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(FilesPath), Times.Once);
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task AddRodo_WriteFailureReturnsFileCreationFailure()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        files.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), false)).ThrowsAsync(new IOException("write failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
        Assert.Empty(db.Rodos);
        files.Verify(file => file.ReadAllBytesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddRodo_DatabaseFailureReturnsDatabaseErrorAndRemovesWrittenFile()
    {
        await using var db = await SeededDb();
        db.FailSaveChanges = true;
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Once);
        files.Verify(file => file.ReadAllBytesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddRodo_DatabaseFailureWithNoWrittenFileReturnsDatabaseErrorWithoutDelete()
    {
        await using var db = await SeededDb();
        db.FailSaveChanges = true;
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Once);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddRodo_CleanupFailureDoesNotMaskDatabaseError()
    {
        await using var db = await SeededDb();
        db.FailSaveChanges = true;
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        files.Setup(file => file.Delete(It.IsAny<string>())).Throws(new IOException("cleanup failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AddRodo_SavedFileReadFailureReturnsFileNotFoundAfterPersistingRodo()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        files.Setup(file => file.ReadAllBytesAsync(It.IsAny<string>())).ThrowsAsync(new IOException("read failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.FileNotFound, error.ErrorCode);
        Assert.Single(db.Rodos);
    }

    [Fact]
    public async Task UpdateRodo_ReplacesDocumentResetsStateAndIncrementsRodoVersion()
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, ".png", 3);
        var files = FileOperations();
        var result = await Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto("replacement.pdf", 3));

        Assert.Equal(TeamMemberRodoState.RODOSTATE_UNVERIFIED, result.RodoState);
        Assert.Equal(4, result.RodoVersion);
        Assert.Equal(".pdf", rodo.Type);
        Assert.Equal(4, rodo.Version);
        Assert.True(rodo.UpdatedAt > rodo.CreatedAt);
        files.Verify(file => file.CreateDirectory(FilesPath), Times.Once);
        files.Verify(file => file.WriteAsync(
            It.Is<string>(path => path.StartsWith(Path.Combine(FilesPath, $"{rodo.Id}_")) && path.EndsWith(".tmp")),
            It.IsAny<IFormFile>(), true), Times.Once);
        files.Verify(file => file.Move(
            It.Is<string>(path => path.StartsWith(Path.Combine(FilesPath, $"{rodo.Id}_")) && path.EndsWith(".tmp")),
            Path.Combine(FilesPath, $"{rodo.Id}.pdf"), true), Times.Once);
        files.Verify(file => file.ReadAllBytesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_MoveFailureReturnsFileCreationFailureAfterDatabaseUpdate()
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, ".png", 2);
        var files = FileOperations();
        files.Setup(file => file.Move(It.IsAny<string>(), It.IsAny<string>(), true))
            .Throws(new IOException("move failed"));

        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(
            TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto("updated.pdf", 2)));

        Assert.Equal(KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
        Assert.Equal(".pdf", rodo.Type);
        Assert.Equal(3, rodo.Version);
        files.Verify(file => file.Move(It.IsAny<string>(), Path.Combine(FilesPath, $"{rodo.Id}.pdf"), true), Times.Once);
    }

    [Fact]
    public async Task UpdateRodo_AnonymousUserIsRejectedBeforeFileOperations()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonGuardianRoleCases))]
    public async Task UpdateRodo_NonGuardianRolesAreRejected(string role)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(Principal(TestDataSeeder.ID_USER_1, role), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRodo_AuthenticatedUserWithoutRoleIsRejected()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(Principal(TestDataSeeder.ID_USER_8), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRodo_GuardianWhoDoesNotOwnTheTeamIsForbidden()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("no-rodo-member")]
    [InlineData("missing-member")]
    public async Task UpdateRodo_MissingMemberOrRodoReturnsRodoNotFound(string target)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var memberId = target == "no-rodo-member" ? TestDataSeeder.ID_TEAM_MEMBER_1 : Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, memberId, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoNotFound, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_BrokenRodoReferenceReturnsRodoNotFound()
    {
        await using var db = await SeededDb();
        var member = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        member!.RodoId = Guid.CreateVersion7();
        await db.SaveChangesAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, member.Id, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.RodoNotFound, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(FilesPath), Times.Once);
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_MissingVersionReturnsVersionConflict()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: null)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(FilesPath), Times.Once);
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public async Task UpdateRodo_StaleVersionReturnsVersionConflict(int version)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, version: 1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: version)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_DirectoryFailureReturnsFolderCreationFailure()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        files.Setup(file => file.CreateDirectory(FilesPath)).Throws(new IOException("directory failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.FolderCreationFailure, error.ErrorCode);
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_WriteFailureReturnsFileCreationFailure()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        files.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), true)).ThrowsAsync(new IOException("write failure"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
        files.Verify(file => file.Exists(It.Is<string>(path => path.EndsWith(".tmp"))), Times.Once);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRodo_DatabaseFailureReturnsDatabaseErrorAndRemovesWrittenFile()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        db.FailSaveChanges = true;
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRodo_RemovesExistingFileAndDatabaseRecord()
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        files.Setup(file => file.Exists(Path.Combine(FilesPath, $"{rodo.Id}.pdf"))).Returns(true);

        await Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);

        Assert.Empty(db.Rodos);
        db.ChangeTracker.Clear();
        var member = await db.TeamMembers.SingleAsync(item => item.Id == TestDataSeeder.ID_TEAM_MEMBER_1);
        Assert.Null(member.RodoId);
        Assert.Equal(TeamMemberRodoState.RODOSTATE_NONE, member.RodoState);
        Assert.Equal(2, member.Version);
        files.Verify(file => file.Exists(Path.Combine(FilesPath, $"{rodo.Id}.pdf")), Times.Once);
        files.Verify(file => file.Delete(Path.Combine(FilesPath, $"{rodo.Id}.pdf")), Times.Once);
    }

    [Fact]
    public async Task DeleteRodo_RemovesDatabaseRecordWhenPhysicalFileIsAlreadyMissing()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();

        await Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);

        Assert.Empty(db.Rodos);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Once);
        files.Verify(file => file.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRodo_AnonymousUserIsRejectedBeforeFileOperations()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonGuardianRoleCases))]
    public async Task DeleteRodo_NonGuardianRolesAreRejected(string role)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(Principal(TestDataSeeder.ID_USER_1, role), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteRodo_AuthenticatedUserWithoutRoleIsRejected()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(Principal(TestDataSeeder.ID_USER_8), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteRodo_GuardianWhoDoesNotOwnTheTeamIsForbidden()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("no-rodo-member")]
    [InlineData("missing-member")]
    public async Task DeleteRodo_MissingMemberOrRodoReturnsRodoNotFound(string target)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var memberId = target == "no-rodo-member" ? TestDataSeeder.ID_TEAM_MEMBER_1 : Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, memberId));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoNotFound, error.ErrorCode);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRodo_BrokenRodoReferenceReturnsRodoNotFound()
    {
        await using var db = await SeededDb();
        var member = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        member!.RodoId = Guid.CreateVersion7();
        await db.SaveChangesAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, member.Id));
        Assert.Equal(KoalaErrorCodes.RodoNotFound, error.ErrorCode);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("exists-fails")]
    [InlineData("delete-fails")]
    public async Task DeleteRodo_PhysicalFileFailureReturnsFileNotFound(string failure)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var path = Path.Combine(FilesPath, $"{rodo.Id}.pdf");
        files.Setup(file => file.Exists(path)).Returns(true);
        if (failure == "exists-fails") files.Setup(file => file.Exists(path)).Throws(new IOException("exists failure"));
        else files.Setup(file => file.Delete(path)).Throws(new IOException("delete failure"));

        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.FileNotFound, error.ErrorCode);
        Assert.Single(db.Rodos);
    }

    [Fact]
    public async Task DeleteRodo_DatabaseFailurePropagatesAfterPhysicalDelete()
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        db.FailSaveChanges = true;
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db, files).DeleteRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal("simulated save failure", error.Message);
        files.Verify(file => file.Delete(Path.Combine(FilesPath, $"{rodo.Id}.pdf")), Times.Once);
    }

    [Theory]
    [MemberData(nameof(AllRoleCases))]
    public async Task GetRodo_RoleMatrixAllowsOnlyOrganizationAdminsAndTeamGuardians(string role)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var principal = role == RoleNames.NAME_TEAM_GUARDIAN ? TeamGuardian() : Principal(TestDataSeeder.ID_USER_1, role);

        if (role is RoleNames.NAME_TEAM_GUARDIAN or RoleNames.NAME_ORGANIZATION_RODO_MODERATOR)
        {
            var result = await Service(db, files).GetRodoAsync(principal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);
            Assert.Equal(TestDataSeeder.ID_TEAM_MEMBER_1, result.TeamMemberId);
            Assert.Equal(1, result.RodoVersion);
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoAsync(principal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
            Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        }
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodo_AnonymousUserIsUnauthorized()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("no-rodo-member")]
    [InlineData("missing-member")]
    public async Task GetRodo_MissingMemberOrRodoReturnsRodoNotFound(string target)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var memberId = target == "no-rodo-member" ? TestDataSeeder.ID_TEAM_MEMBER_1 : Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, memberId));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoNotFound, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodo_BrokenRodoReferenceReturnsRodoNotFound()
    {
        await using var db = await SeededDb();
        var member = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        member!.RodoId = Guid.CreateVersion7();
        await db.SaveChangesAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, member.Id));
        Assert.Equal(KoalaErrorCodes.RodoNotFound, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodo_GuardianOutsideTeamIsForbidden()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AllRoleCases))]
    public async Task GetRodoFile_RoleMatrixAllowsOnlyOrganizationAdminsAndTeamGuardians(string role)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, ".webp");
        var files = FileOperations();
        var principal = role == RoleNames.NAME_TEAM_GUARDIAN ? TeamGuardian() : Principal(TestDataSeeder.ID_USER_1, role);

        if (role is RoleNames.NAME_TEAM_GUARDIAN or RoleNames.NAME_ORGANIZATION_RODO_MODERATOR)
        {
            var result = await Service(db, files).GetRodoFileAsync(principal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);
            Assert.Equal(Path.Combine(FilesPath, $"{rodo.Id}.webp"), result);
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoFileAsync(principal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
            Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        }
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodoFile_AnonymousUserIsUnauthorized()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoFileAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("no-rodo-member")]
    [InlineData("missing-member")]
    public async Task GetRodoFile_MissingMemberOrRodoReturnsRodoNotFound(string target)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var memberId = target == "no-rodo-member" ? TestDataSeeder.ID_TEAM_MEMBER_1 : Guid.NewGuid();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoFileAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, memberId));
        Assert.Equal(KoalaErrorCodes.TeamMemberRodoNotFound, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodoFile_BrokenRodoReferenceReturnsRodoNotFound()
    {
        await using var db = await SeededDb();
        var member = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        member!.RodoId = Guid.CreateVersion7();
        await db.SaveChangesAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoFileAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, member.Id));
        Assert.Equal(KoalaErrorCodes.RodoNotFound, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodoFile_GuardianOutsideTeamIsForbidden()
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodoFileAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AllRoleCases))]
    public async Task GetRodos_RoleMatrixAllowsOnlyOrganizationAdminsAndTeamGuardians(string role)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var principal = role == RoleNames.NAME_TEAM_GUARDIAN ? TeamGuardian() : Principal(TestDataSeeder.ID_USER_1, role);

        if (role is RoleNames.NAME_TEAM_GUARDIAN or RoleNames.NAME_ORGANIZATION_RODO_MODERATOR)
        {
            var (members, page) = await Service(db, files).GetRodosAsync(principal, TestDataSeeder.ID_TEAM_1, new PageQueryDto(0, 10));
            Assert.Equal(3, members.Count);
            Assert.Equal(3, page.TotalNumber);
            Assert.All(members, member => Assert.Null(member.RodoVersion));
        }
        else
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodosAsync(principal, TestDataSeeder.ID_TEAM_1, new PageQueryDto(0, 10)));
            Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        }
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodos_AnonymousUserIsUnauthorized()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodosAsync(null, TestDataSeeder.ID_TEAM_1, new PageQueryDto()));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodos_GuardianOutsideTeamIsForbidden()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).GetRodosAsync(NonMemberGuardian(), TestDataSeeder.ID_TEAM_1, new PageQueryDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRodos_OrganizationAdminCanReadOtherTeamsAndEmptyResult()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var (members, pagination) = await Service(db, files).GetRodosAsync(OrganizationAdmin(), Guid.NewGuid(), new PageQueryDto());
        Assert.Empty(members);
        Assert.Equal(0, pagination.TotalNumber);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    public async Task GetRodos_AppliesPaginationAfterFilteringByTeam(int pageNumber, int pageSize)
    {
        await using var db = await SeededDb();
        var (members, pagination) = await Service(db, FileOperations()).GetRodosAsync(TeamGuardian(), TestDataSeeder.ID_TEAM_1, new PageQueryDto(pageNumber, pageSize));
        Assert.Equal(Math.Min(pageSize, Math.Max(0, 3 - pageNumber * pageSize)), members.Count);
        Assert.Equal(3, pagination.TotalNumber);
        Assert.Equal(pageNumber, pagination.PageNumber);
        Assert.Equal(pageSize, pagination.PageSize);
    }

    [Theory]
    [MemberData(nameof(NonOrganizationAdminRoleCases))]
    public async Task SetRodoState_NonOrganizationAdminsAreForbidden(string role)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(Principal(TestDataSeeder.ID_USER_1, role), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 1, Version: 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetRodoState_AnonymousUserIsUnauthorized()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 1, Version: 1)));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(TeamMemberRodoState.RODOSTATE_ACCEPETED)]
    [InlineData(TeamMemberRodoState.RODOSTATE_REJECTED)]
    public async Task SetRodoState_OrganizationAdminUpdatesStateAndIncrementsMemberVersion(string state)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, version: 3);
        var files = FileOperations();
        var result = await Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(state, RodoVersion: rodo.Version, Version: 1));
        var saved = await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1);
        Assert.Equal(state, result.RodoState);
        Assert.Equal(state, saved!.RodoState);
        Assert.Equal(2, result.Version);
        Assert.Equal(2, saved.Version);
        Assert.Equal(3, result.RodoVersion);
        Assert.Equal(3, rodo.Version);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetRodoState_MissingTeamMemberReturnsNotFound()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, Guid.NewGuid(), new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 1, Version: 1)));
        Assert.Equal(KoalaErrorCodes.TeamMemberNotFound, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public async Task SetRodoState_StaleVersionReturnsConflict(int version)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_REJECTED, RodoVersion: rodo.Version, Version: version)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
        Assert.Equal(1, (await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1))!.Version);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetRodoState_DatabaseFailurePropagates()
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        db.FailSaveChanges = true;
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_REJECTED, RodoVersion: rodo.Version, Version: 1)));
        Assert.Equal("simulated save failure", error.Message);
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetRodoState_MemberWithoutDocumentReturnsNotFound()
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 1, Version: 1)));
        Assert.Equal(KoalaErrorCodes.TeamMemberNotFound, error.ErrorCode);
        Assert.Equal(1, (await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1))!.Version);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task SetRodoState_StaleDocumentVersionDoesNotChangeMember(int rodoVersion)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, version: 3);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).SetRodoStateAsync(OrganizationAdmin(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_REJECTED, RodoVersion: rodoVersion, Version: 1)));
        Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, error.ErrorCode);
        var member = (await db.TeamMembers.FindAsync(TestDataSeeder.ID_TEAM_MEMBER_1))!;
        Assert.Equal(1, member.Version);
        Assert.Equal(TeamMemberRodoState.RODOSTATE_ACCEPETED, member.RodoState);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(SeededReadRoleCases))]
    public async Task GetRodo_SeededUsersAreAuthorizedByTheirAssignedRole(Guid userId, string[] roles, bool canRead)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var action = () => Service(db, files).GetRodoAsync(Principal(userId, roles), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);

        if (canRead) Assert.Equal(TestDataSeeder.ID_TEAM_MEMBER_1, (await action()).TeamMemberId);
        else Assert.Equal(KoalaErrorCodes.Forbidden, (await Assert.ThrowsAsync<KoalaException>(action)).ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(SeededReadRoleCases))]
    public async Task GetRodoFile_SeededUsersAreAuthorizedByTheirAssignedRole(Guid userId, string[] roles, bool canRead)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var action = () => Service(db, files).GetRodoFileAsync(Principal(userId, roles), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1);

        if (canRead) Assert.Equal(Path.Combine(FilesPath, $"{rodo.Id}.pdf"), await action());
        else Assert.Equal(KoalaErrorCodes.Forbidden, (await Assert.ThrowsAsync<KoalaException>(action)).ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(SeededReadRoleCases))]
    public async Task GetRodos_SeededUsersAreAuthorizedByTheirAssignedRole(Guid userId, string[] roles, bool canRead)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var action = () => Service(db, files).GetRodosAsync(Principal(userId, roles), TestDataSeeder.ID_TEAM_1, new PageQueryDto());

        if (canRead) Assert.Equal(3, (await action()).Item1.Count);
        else Assert.Equal(KoalaErrorCodes.Forbidden, (await Assert.ThrowsAsync<KoalaException>(action)).ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonOwnerGuardianUsers))]
    public async Task AddRodo_NonOwnerGuardianCannotAddToAnotherUsersTeam(Guid userId)
    {
        await using var db = await SeededDb();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).AddRodoAsync(Principal(userId, RoleNames.NAME_TEAM_GUARDIAN), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto()));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(NonOwnerGuardianUsers))]
    public async Task UpdateRodo_NonOwnerGuardianCannotUpdateAnotherUsersTeam(Guid userId)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).UpdateRodoAsync(Principal(userId, RoleNames.NAME_TEAM_GUARDIAN), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, UploadDto(version: 1)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(NonOwnerGuardianUsers))]
    public async Task DeleteRodo_NonOwnerGuardianCannotDeleteAnotherUsersTeam(Guid userId)
    {
        await using var db = await SeededDb();
        await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1);
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => Service(db, files).DeleteRodoAsync(Principal(userId, RoleNames.NAME_TEAM_GUARDIAN), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.Verify(file => file.Exists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(AllowedRodoUploads))]
    public async Task UpdateRodo_ReplacesFileForEverySupportedExtension(string extension, string contentType)
    {
        await using var db = await SeededDb();
        var rodo = await AddRodoAsync(db, TestDataSeeder.ID_TEAM_MEMBER_1, ".pdf", version: 4);
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        var result = await Service(db, files).UpdateRodoAsync(
            TeamGuardian(), TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1,
            UploadDto($"updated{extension}", version: 4, contentType));

        Assert.Equal(extension, (await db.Rodos.SingleAsync()).Type);
        Assert.Equal(5, rodo.Version);
        Assert.Equal(TeamMemberRodoState.RODOSTATE_UNVERIFIED, result.RodoState);
        files.Verify(file => file.WriteAsync(It.Is<string>(path => path.EndsWith(".tmp")), It.IsAny<IFormFile>(), true), Times.Once);
        files.Verify(file => file.Move(It.Is<string>(path => path.EndsWith(".tmp")), It.Is<string>(path => path.EndsWith($"{rodo.Id}{extension}")), true), Times.Once);
        files.Verify(file => file.Delete(It.Is<string>(path => path.EndsWith($"{rodo.Id}.pdf"))), extension == ".pdf" ? Times.Never : Times.Once);
    }

}

