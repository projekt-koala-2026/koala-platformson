using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class CreatePublicFileRequestDtoValidationTests
{
    private const string ContentTypeError = "File content type is not supported.";
    private const string ExtensionError = "Invalid file extension. Allowed extensions are: .txt, .csv, .docs, .doc, .docx, .xls, .xlsx, .xlsm, .xlsb, .jpg, .jpeg, .png, .webp, .pdf";
    private const string TooLargeError = "File size cannot exceed 10 MB!";
    private static CreatePublicFileRequestDtoValidator Validator => new();

    private static IFormFile FormFile(long length = 1, string fileName = "file.txt", string contentType = "text/plain")
    {
        var bytes = new byte[checked((int)length)];
        return new FormFile(new MemoryStream(bytes), 0, length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static CreatePublicFileRequestDto Model(IFormFile? file = null, string? description = null) => new("Public resource", description, file ?? FormFile());

    [Fact]
    public void AcceptsValidTextFile() => ValidationAssertions.IsValid(Validator, Model());

    [Theory]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("table.csv", "text/csv")]
    [InlineData("legacy.docs", "application/msword")]
    [InlineData("legacy.doc", "application/msword")]
    [InlineData("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("legacy.xls", "application/vnd.ms-excel")]
    [InlineData("table.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("macro.xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12")]
    [InlineData("binary.xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("document.pdf", "application/pdf")]
    public void AcceptsEveryConfiguredExtension(string fileName, string contentType) =>
        ValidationAssertions.IsValid(Validator, Model(FormFile(fileName: fileName, contentType: contentType)));

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(10 * 1024 * 1024 - 1)]
    [InlineData(10 * 1024 * 1024)]
    public void AcceptsNonEmptyFilesUpToAndIncludingTenMegabytes(int length) =>
        ValidationAssertions.IsValid(Validator, Model(FormFile(length: length)));

    [Fact]
    public void RejectsNullFileWithFileEmptyCode()
    {
        ValidationAssertions.HasError(Validator, new CreatePublicFileRequestDto("Public resource", null, null!),
            KoalaValidationErrorCodes.FileEmpty, "File cannot be empty!");
    }

    [Fact]
    public void AcceptsMissingOrNonEmptyDescription()
    {
        ValidationAssertions.IsValid(Validator, Model());
        ValidationAssertions.IsValid(Validator, Model(description: "Reference material"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyProvidedDescription(string description) =>
        ValidationAssertions.HasError(Validator, Model(description: description),
            KoalaValidationErrorCodes.PublicFileDescriptionEmpty, "Description field cannot be empty when not null!");

    [Fact]
    public void RejectsZeroLengthFileWithFileEmptyCode()
    {
        ValidationAssertions.HasError(Validator, Model(FormFile(length: 0)),
            KoalaValidationErrorCodes.FileEmpty, "File cannot be empty!");
    }

    [Theory]
    [InlineData(10 * 1024 * 1024 + 1)]
    [InlineData(11 * 1024 * 1024)]
    public void RejectsFilesLargerThanTenMegabytes(int length) =>
        ValidationAssertions.HasError(Validator, Model(FormFile(length: length)),
            KoalaValidationErrorCodes.FileToLarge, TooLargeError);

    [Theory]
    [InlineData("file.gif")]
    [InlineData("file.exe")]
    [InlineData("file")]
    [InlineData("file.txt.exe")]
    public void RejectsUnsupportedExtensions(string fileName) =>
        ValidationAssertions.HasError(Validator, Model(FormFile(fileName: fileName)),
            KoalaValidationErrorCodes.FileInvalidType, ExtensionError);

    [Theory]
    [InlineData("file.txt", "application/x-executable")]
    [InlineData("file.xlsx", "application/octet-stream")]
    public void RejectsUnsupportedContentTypes(string fileName, string contentType) =>
        ValidationAssertions.HasError(Validator, Model(FormFile(fileName: fileName, contentType: contentType)),
            KoalaValidationErrorCodes.FileInvalidType, ContentTypeError);

    [Theory]
    [InlineData("FILE.PDF", "APPLICATION/PDF")]
    [InlineData("PHOTO.JpEg", "IMAGE/JPEG")]
    [InlineData("TABLE.CsV", "TEXT/CSV")]
    public void ExtensionAndContentTypeChecksAreCaseInsensitive(string fileName, string contentType) =>
        ValidationAssertions.IsValid(Validator, Model(FormFile(fileName: fileName, contentType: contentType)));

    [Fact]
    public void StopsAfterFirstFileValidationFailure()
    {
        var result = Validator.Validate(Model(FormFile(length: 0, fileName: "bad.exe", contentType: "application/x-executable")));
        Assert.Collection(result.Errors,
            error =>
            {
                Assert.Equal(KoalaValidationErrorCodes.FileEmpty, error.ErrorCode);
                Assert.Equal("File cannot be empty!", error.ErrorMessage);
            });
    }
}
