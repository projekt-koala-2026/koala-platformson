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

    public static IEnumerable<object[]> SupportedUploadFiles => new (string Extension, string ContentType)[]
    {
        (".txt", "text/plain"), (".csv", "text/csv"), (".docs", "application/msword"),
        (".doc", "application/msword"), (".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        (".xls", "application/vnd.ms-excel"), (".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        (".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"), (".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12"),
        (".jpg", "image/jpeg"), (".jpeg", "image/jpeg"), (".png", "image/png"), (".webp", "image/webp"), (".pdf", "application/pdf")
    }.Select(x => new object[] { x.Extension, x.ContentType });

    public static TheoryData<string, string, string> SupportedFilesAndEditorRoles()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var file in SupportedUploadFiles)
        {
            foreach (var role in EditorRoles)
            {
                data.Add((string)file[0], (string)file[1], (string)role[0]);
            }
        }

        return data;
    }

    private static Mock<IPublicFileOperations> FileOperations()
    {
        var operations = new Mock<IPublicFileOperations>(MockBehavior.Strict);
        operations.Setup(file => file.CreateDirectory(It.IsAny<string>()));
        operations.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>())).Returns(Task.CompletedTask);
        operations.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);
        operations.Setup(file => file.Delete(It.IsAny<string>()));
        return operations;
    }

    private static IFormFile FormFile(string name = "upload.png", string contentType = "image/png", byte[]? contents = null)
    {
        var bytes = contents ?? Encoding.UTF8.GetBytes("file contents");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Theory]
    [MemberData(nameof(SupportedFilesAndEditorRoles))]
    public async Task AddFile_AcceptsEachConfiguredPublicFileType(string extension, string contentType, string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var upload = FormFile($"upload{extension}", contentType);
        var result = await new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("upload", upload));
        Assert.Equal(1, result.Version);
        Assert.Equal(extension, result.Type);
        Assert.EndsWith(extension, result.Path);
        Assert.Equal(3, db.PublicFiles.Count());
        files.Verify(file => file.CreateDirectory("public"), Times.Once);
        files.Verify(file => file.WriteAsync(It.Is<string>(path => path.EndsWith(extension)), upload), Times.Once);
    }

    [Fact]
    public async Task AddFile_AnonymousRequestIsRejectedBeforeFileSystemAccess()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "", files.Object).AddFileAsync(null, new CreatePublicFileRequestDto("x", FormFile())));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task AddFile_NonEditorRolesAreForbidden(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("x", FormFile())));
        Assert.Equal(KoalaErrorCodes.Forbidden, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task AddFile_FileSystemFailureReturnsFolderCreationFailure(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        files.Setup(file => file.CreateDirectory("blocked")).Throws(new IOException("create directory failed"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "blocked", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("x", FormFile())));
        Assert.Equal(KoalaErrorCodes.FolderCreationFailure, error.ErrorCode);
        Assert.Equal(2, db.PublicFiles.Count());
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task AddFile_CopyFailureReturnsFileCreationFailure(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var brokenFile = new Mock<IFormFile>();
        brokenFile.SetupGet(file => file.FileName).Returns("broken.png");
        brokenFile.Setup(file => file.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("copy failed"));
        files.Setup(file => file.WriteAsync(It.IsAny<string>(), brokenFile.Object)).ThrowsAsync(new IOException("copy failed"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("broken", brokenFile.Object)));
        Assert.Equal(KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
        Assert.Equal(2, db.PublicFiles.Count());
    }

    [Theory]
    [MemberData(nameof(EditorRoles))]
    public async Task AddFile_DatabaseFailureReturnsDatabaseErrorAndRemovesUploadedFile(string role)
    {
        var files = FileOperations();
        files.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
        var options = new DbContextOptionsBuilder<CmsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new FailingSaveCmsDbContext(options);
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("upload", FormFile())));
        Assert.Equal(KoalaErrorCodes.DatabaseError, error.ErrorCode);
        files.Verify(file => file.Delete(It.Is<string>(path => path.StartsWith("public"))), Times.Once);
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
        var files = FileOperations();
        await new PublicFileService(db, "public", files.Object).DeleteFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), TestDataSeeder.ID_PUBLIC_FILE_1);
        Assert.Null(await db.PublicFiles.FindAsync(TestDataSeeder.ID_PUBLIC_FILE_1));
        files.Verify(file => file.Delete(Path.Combine("public", TestDataSeeder.PATH_PUBLIC_FILE_1)), Times.Once);
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
