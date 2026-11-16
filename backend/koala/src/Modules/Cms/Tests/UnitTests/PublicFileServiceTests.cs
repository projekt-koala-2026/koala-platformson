using System.Text;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace koala.src.Modules.Cms.Tests.UnitTests;

public class PublicFileServiceTests
{
    private sealed class FailingSaveCmsDbContext(DbContextOptions<CmsDbContext> options) : CmsDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<int>(new InvalidOperationException("simulated database failure"));
    }

    public static IEnumerable<object[]> EditorRoles => new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR }.Select(role => new object[] { role });
    public static IEnumerable<object[]> ForbiddenRoles => CmsTestRoles.NonEditorRoles.Select(role => new object[] { role });

    private static IFormFile FormFile(string name = "upload.png", byte[]? contents = null)
    {
        var bytes = contents ?? Encoding.UTF8.GetBytes("file contents");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task AddFile_AdminAndEditorCanUploadAndFileStartsAtVersionOne(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var result = await new PublicFileService(db, directory).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("upload", FormFile()));
            Assert.Equal(1, result.Version);
            Assert.EndsWith(".png", result.Path);
            Assert.True(File.Exists(Path.Combine(directory, result.Path)));
            Assert.Equal(3, db.PublicFiles.Count());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task AddFile_AnonymousRequestIsRejectedBeforeFileSystemAccess()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").AddFileAsync(null, new CreatePublicFileRequestDto("x", FormFile())));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task AddFile_NonEditorRolesAreForbidden(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("x", FormFile())));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Fact]
    public async Task AddFile_FileSystemFailureReturnsFolderCreationFailure()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var parent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        var notDirectory = Path.Combine(parent, "file-blocker");
        await File.WriteAllTextAsync(notDirectory, "block");
        try
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, notDirectory).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), new CreatePublicFileRequestDto("x", FormFile())));
            Assert.Equal(KoalaErrorCodes.FolderCreationFailure, error.ErrorCode);
            Assert.Equal(2, db.PublicFiles.Count());
        }
        finally
        {
            Directory.Delete(parent, true);
        }
    }

    [Fact]
    public async Task AddFile_CopyFailureReturnsFileCreationFailure()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var brokenFile = new Mock<IFormFile>();
        brokenFile.SetupGet(file => file.FileName).Returns("broken.png");
        brokenFile.Setup(file => file.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("copy failed"));
        try
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, directory).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_EDITOR]), new CreatePublicFileRequestDto("broken", brokenFile.Object)));
            Assert.Equal(KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
            Assert.Equal(2, db.PublicFiles.Count());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task AddFile_DatabaseFailureReturnsDatabaseErrorAndRemovesUploadedFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new DbContextOptionsBuilder<CmsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new FailingSaveCmsDbContext(options);
        try
        {
            var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, directory).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [RoleNames.NAME_ORGANIZATION_ADMIN]), new CreatePublicFileRequestDto("upload", FormFile())));
            Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task DeleteFile_AnonymousRequestIsRejected()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").DeleteFileAsync(null, TestDataSeeder.ID_PUBLIC_FILE_1));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task DeleteFile_NonEditorRolesAreForbidden(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").DeleteFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_PUBLIC_FILE_1));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task DeleteFile_MissingIdReturnsFileNotFound(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").DeleteFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), Guid.NewGuid()));
        Assert.Equal(KoalaErrorCodes.FileNotFound, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task DeleteFile_RemovesDatabaseRowAndExistingPhysicalFile(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, TestDataSeeder.PATH_PUBLIC_FILE_1), "test file");
        try
        {
            await new PublicFileService(db, directory).DeleteFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_PUBLIC_FILE_1);
            Assert.Null(await db.PublicFiles.FindAsync(TestDataSeeder.ID_PUBLIC_FILE_1));
            Assert.False(File.Exists(Path.Combine(directory, TestDataSeeder.PATH_PUBLIC_FILE_1)));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task GetFiles_AnonymousRequestIsRejected()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").GetFilesAsync(null, new PageQueryDto(), new PublicFileQueryDto(null, null)));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task GetFiles_NonEditorRolesAreForbidden(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "").GetFilesAsync(CmsTestPrincipal.Authenticated(roles: [role]), new PageQueryDto(), new PublicFileQueryDto(null, null)));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task GetFiles_FiltersByTypeAndNameAndPaginates(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var (items, pagination) = await new PublicFileService(db, "").GetFilesAsync(CmsTestPrincipal.Authenticated(roles: [role]), new PageQueryDto(0, 1), new PublicFileQueryDto("image/png", TestDataSeeder.NAME_PUBLIC_FILE_1));
        Assert.Single(items);
        Assert.Equal(TestDataSeeder.ID_PUBLIC_FILE_1, items[0].Id);
        Assert.Equal(1, pagination.TotalNumber);
    }
}
