using System.Text;
using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
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

    private sealed class ToggleFailSaveCmsDbContext(DbContextOptions<CmsDbContext> options) : CmsDbContext(options)
    {
        public bool FailSaveChanges { get; set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            FailSaveChanges ? Task.FromException<int>(new InvalidOperationException("simulated database failure")) : base.SaveChangesAsync(cancellationToken);
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
        operations.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        operations.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);
        operations.Setup(file => file.Delete(It.IsAny<string>()));
        operations.Setup(file => file.Move(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
        operations.Setup(file => file.ReadAllBytesAsync(It.IsAny<string>())).ReturnsAsync(Array.Empty<byte>());
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
        var result = await new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("upload", null, upload));
        Assert.Equal(1, result.Version);
        Assert.Equal(extension, result.Type);
        Assert.EndsWith(extension, result.Path);
        Assert.Equal(3, db.PublicFiles.Count());
        files.Verify(file => file.CreateDirectory("public"), Times.Once);
        files.Verify(file => file.WriteAsync(Path.Combine("public", result.Path), upload, true), Times.Once);
    }

    [Fact]
    public async Task AddFile_AnonymousRequestIsRejectedBeforeFileSystemAccess()
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "", files.Object).AddFileAsync(null, new CreatePublicFileRequestDto("x", null, FormFile())));
        Assert.Equal(KoalaErrorCodes.Unauthenticated, error.ErrorCode);
        files.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ForbiddenRoles))]
    public async Task AddFile_NonEditorRolesAreForbidden(string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var files = FileOperations();
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("x", null, FormFile())));
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
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "blocked", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("x", null, FormFile())));
        Assert.Equal(KoalaErrorCodes.FolderCreationFailure, error.ErrorCode);
        Assert.Equal(2, db.PublicFiles.Count());
        files.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
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
        files.Setup(file => file.WriteAsync(It.IsAny<string>(), brokenFile.Object, It.IsAny<bool>())).ThrowsAsync(new IOException("copy failed"));
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("broken", null, brokenFile.Object)));
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
        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "public", files.Object).AddFileAsync(CmsTestPrincipal.Authenticated(roles: [role]), new CreatePublicFileRequestDto("upload", null, FormFile())));
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

    private static readonly string[] PreviousFileTypes = [".png", ".pdf"];
    private static readonly string[] ReplacementFileTypes = [".png", ".pdf", ".txt", ".csv", ".doc", ".docx", ".xls", ".xlsx", ".xlsm", ".xlsb"];

    public static TheoryData<string, string, string> FileReplacementCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var oldType in PreviousFileTypes)
        foreach (var newType in ReplacementFileTypes)
        foreach (var role in EditorRoles)
            data.Add(oldType, newType, (string)role[0]);
        return data;
    }

    public static TheoryData<string, string, string> MetadataUpdateCases()
    {
        var data = new TheoryData<string, string, string>();
        for (var i = 0; i < 16; i++)
        foreach (var role in EditorRoles)
        {
            var description = (i % 4) switch
            {
                0 => null,
                1 => $"Reference file {i}",
                2 => $"Zażółć-{i}-🙂",
                _ => $"https://example.test/files/{i}?v={i + 1}"
            };
            data.Add((string)role[0], $"Metadata name {i:D2}", description ?? "<null>");
        }
        return data;
    }

    public static TheoryData<string, string> ReplacementFailureCases()
    {
        var data = new TheoryData<string, string>();
        foreach (var stage in new[] { "write", "save", "move" })
        foreach (var role in EditorRoles)
            data.Add(stage, (string)role[0]);
        return data;
    }

    public static IEnumerable<object[]> MetadataSaveFailureCases => EditorRoles.Select(role => new object[] { (string)role[0] });

    public static TheoryData<string, string, string> UpdateAccessCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var operation in new[] { "metadata", "replace" })
        {
            data.Add(operation, "anonymous", "auth");
            foreach (var role in CmsTestRoles.NonEditorRoles)
                data.Add(operation, role, "forbidden");
            foreach (var role in EditorRoles)
            {
                data.Add(operation, (string)role[0], "missing-empty");
                data.Add(operation, (string)role[0], "missing-random");
                if (operation == "replace")
                    data.Add(operation, (string)role[0], "folder-failure");
            }
        }
        return data;
    }

    private static async Task<PublicFile> AddStoredFileAsync(CmsDbContext db, string extension)
    {
        var now = DateTime.UtcNow;
        var id = Guid.CreateVersion7();
        var row = new PublicFile
        {
            Id = id,
            Name = "Original name",
            Description = "Original description",
            Path = $"{id}{extension}",
            Type = extension,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 4
        };
        db.PublicFiles.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    [Theory]
    [MemberData(nameof(FileReplacementCases))]
    public async Task UpdateFile_ReplacesExistingUploadAcrossExtensionAndEditorRoleMatrix(string oldType, string newType, string role)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var stored = await AddStoredFileAsync(db, oldType);
        var operations = FileOperations();
        var oldPath = Path.Combine("public", $"{stored.Id}{oldType}");
        var newPath = Path.Combine("public", $"{stored.Id}{newType}");
        operations.Setup(file => file.Exists(oldPath)).Returns(true);
        var upload = FormFile($"replacement{newType}", "application/octet-stream", Encoding.UTF8.GetBytes($"replacement {oldType} {newType} {role}"));
        var beforeUpdate = stored.UpdatedAt;

        var result = await new PublicFileService(db, "public", operations.Object).UpdateFileAsync(
            CmsTestPrincipal.Authenticated(roles: [role]), stored.Id,
            new CreatePublicFileRequestDto("Replacement name", "Replacement description", upload));

        Assert.Equal(stored.Id, result.Id);
        Assert.Equal("Replacement name", result.Name);
        Assert.Equal("Replacement description", result.Description);
        Assert.Equal(newType, result.Type);
        Assert.Equal($"{stored.Id}{newType}", result.Path);
        Assert.Equal(5, result.Version);
        Assert.True(result.UpdatedAt >= beforeUpdate);
        Assert.Equal("Replacement name", (await db.PublicFiles.FindAsync(stored.Id))!.Name);
        operations.Verify(file => file.WriteAsync(It.Is<string>(path => path.StartsWith(Path.Combine("public", $"{stored.Id}_")) && path.EndsWith(".tmp")), upload, true), Times.Once);
        operations.Verify(file => file.Move(It.Is<string>(path => path.EndsWith(".tmp")), newPath, true), Times.Once);
        if (oldType == newType)
            operations.Verify(file => file.Delete(It.IsAny<string>()), Times.Never);
        else
            operations.Verify(file => file.Delete(oldPath), Times.Once);
    }

    [Theory]
    [MemberData(nameof(MetadataUpdateCases))]
    public async Task UpdateFileMetaData_ChangesOnlyMetadataAndAdvancesVersion(string role, string name, string description)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var stored = await AddStoredFileAsync(db, ".pdf");
        var originalPath = stored.Path;
        var originalType = stored.Type;
        var originalCreatedAt = stored.CreatedAt;
        var operations = FileOperations();
        var service = new PublicFileService(db, "public", operations.Object);

        var result = await service.UpdateFileMetaDataAsync(
            CmsTestPrincipal.Authenticated(roles: [role]), stored.Id,
            new UpdatePublicFileRequestDto(name, description == "<null>" ? null : description));

        Assert.Equal(name, result.Name);
        Assert.Equal(description == "<null>" ? null : description, result.Description);
        Assert.Equal(originalPath, result.Path);
        Assert.Equal(originalType, result.Type);
        Assert.Equal(originalCreatedAt, result.CreatedAt);
        Assert.Equal(5, result.Version);
        var persisted = await db.PublicFiles.FindAsync(stored.Id);
        Assert.Equal(name, persisted!.Name);
        Assert.Equal(result.Description, persisted.Description);
        operations.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
        operations.Verify(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<bool>()), Times.Never);
        operations.Verify(file => file.Move(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        operations.Verify(file => file.Delete(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ReplacementFailureCases))]
    public async Task UpdateFile_MapsWriteDatabaseAndMoveFailures(string stage, string role)
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ToggleFailSaveCmsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedDb(db);
        var stored = await AddStoredFileAsync(db, ".png");
        var operations = FileOperations();
        var tempPathPrefix = Path.Combine("public", $"{stored.Id}_");
        if (stage == "write")
            operations.Setup(file => file.WriteAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), true)).ThrowsAsync(new IOException("write failed"));
        if (stage == "move")
            operations.Setup(file => file.Move(It.IsAny<string>(), It.IsAny<string>(), true)).Throws(new IOException("move failed"));
        operations.Setup(file => file.Exists(It.Is<string>(path => path.StartsWith(tempPathPrefix)))).Returns(true);
        if (stage == "save")
            db.FailSaveChanges = true;

        var error = await Assert.ThrowsAsync<KoalaException>(() => new PublicFileService(db, "public", operations.Object).UpdateFileAsync(
            CmsTestPrincipal.Authenticated(roles: [role]), stored.Id,
            new CreatePublicFileRequestDto("Replacement", "Replacement", FormFile("replacement.pdf"))));

        Assert.Equal(stage == "save" ? KoalaErrorCodes.DatabaseError : KoalaErrorCodes.FileCreationFailure, error.ErrorCode);
        if (stage is "write" or "save")
            operations.Verify(file => file.Delete(It.Is<string>(path => path.StartsWith(tempPathPrefix))), Times.Once);
        if (stage == "move")
        {
            Assert.Equal(5, (await db.PublicFiles.FindAsync(stored.Id))!.Version);
            Assert.Equal(".pdf", (await db.PublicFiles.FindAsync(stored.Id))!.Type);
        }
    }

    [Theory]
    [MemberData(nameof(MetadataSaveFailureCases))]
    public async Task UpdateFileMetaData_DatabaseFailureIsSwallowedByCurrentServiceImplementation(string role)
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ToggleFailSaveCmsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedDb(db);
        var stored = await AddStoredFileAsync(db, ".pdf");
        var operations = FileOperations();
        db.FailSaveChanges = true;

        var result = await new PublicFileService(db, "public", operations.Object)
            .UpdateFileMetaDataAsync(CmsTestPrincipal.Authenticated(roles: [role]), stored.Id, new UpdatePublicFileRequestDto("Changed", null));

        Assert.Equal("Changed", result.Name);
        Assert.Equal("Original name", (await db.PublicFiles.AsNoTracking().SingleAsync(file => file.Id == stored.Id)).Name);
        operations.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(UpdateAccessCases))]
    public async Task PublicFileUpdates_EnforceAuthenticationAuthorizationLookupAndFolderCreation(string operation, string role, string scenario)
    {
        await using var db = await CmsTestDatabase.CreateSeededAsync();
        var allowed = role is RoleNames.NAME_ORGANIZATION_ADMIN or RoleNames.NAME_ORGANIZATION_EDITOR;
        var stored = await AddStoredFileAsync(db, ".png");
        var operations = FileOperations();
        var requestId = scenario switch
        {
            "missing-empty" => Guid.Empty,
            "missing-random" => Guid.NewGuid(),
            _ => stored.Id
        };
        if (scenario == "folder-failure")
            operations.Setup(file => file.CreateDirectory("blocked")).Throws(new IOException("directory failure"));

        ClaimsPrincipal? principal = scenario == "auth"
            ? null
            : role == "no-role"
                ? CmsTestPrincipal.Authenticated()
                : CmsTestPrincipal.Authenticated(roles: [role]);
        var service = new PublicFileService(db, scenario == "folder-failure" ? "blocked" : "public", operations.Object);

        var error = await Assert.ThrowsAsync<KoalaException>(() => operation == "metadata"
            ? service.UpdateFileMetaDataAsync(principal, requestId, new UpdatePublicFileRequestDto("Updated", null))
            : service.UpdateFileAsync(principal, requestId, new CreatePublicFileRequestDto("Updated", null, FormFile())));

        var expected = scenario switch
        {
            "auth" => KoalaErrorCodes.Unauthenticated,
            "forbidden" => KoalaErrorCodes.Forbidden,
            "missing-empty" or "missing-random" => KoalaErrorCodes.PublicFileNotFound,
            "folder-failure" => KoalaErrorCodes.FolderCreationFailure,
            _ => throw new InvalidOperationException("Unexpected test scenario")
        };
        Assert.Equal(expected, error.ErrorCode);
        if (!allowed || scenario == "auth")
            operations.Verify(file => file.CreateDirectory(It.IsAny<string>()), Times.Never);
        if (scenario is "missing-empty" or "missing-random")
            operations.Verify(file => file.CreateDirectory(It.IsAny<string>()), operation == "replace" ? Times.Once : Times.Never);
    }
}
