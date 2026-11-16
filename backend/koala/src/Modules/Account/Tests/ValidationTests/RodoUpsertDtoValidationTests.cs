using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class RodoUpsertDtoValidationTests
{
    private const string EmptyFileMessage = "File cannot be empty!";
    private const string TooLargeMessage = "File size cannot exceed 2 MB!";
    private const string ContentTypeMessage = "Only image files (JPEG, PNG, WebP) and PDF files are allowed!";
    private const string ExtensionMessage = "Invalid file extension. Allowed extensions are: .jpg, .jpeg, .png, .webp, .pdf";
    private readonly RodoUpsertDtoValidator _validator = new();

    private static IFormFile FormFile(int length = 1, string name = "file.pdf", string contentType = "application/pdf")
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "File", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("photo.pdf", "application/pdf")]
    [InlineData("PHOTO.JPG", "IMAGE/JPEG")]
    public void SupportedExtensionsAndMediaTypes_AreAccepted(string filename, string contentType) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(name: filename, contentType: contentType), null));

    [Fact]
    public void MaximumFileSizeOf2MiB_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new(FormFile(length: 2 * 1024 * 1024), 1));

    [Fact]
    public void EmptyFile_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(length: 0), null), EmptyFileMessage);

    [Fact]
    public void FileOneByteOver2MiB_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(length: 2 * 1024 * 1024 + 1), null), TooLargeMessage);

    [Fact]
    public void UnsupportedContentType_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(contentType: "text/plain"), null), ContentTypeMessage);

    [Fact]
    public void UnsupportedExtension_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(name: "photo.gif", contentType: "image/png"), null), ExtensionMessage);

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void NullOrPositiveVersion_IsAccepted(int? version) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(), version));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveOptionalVersion_ReturnsConfiguredMessage(int version) =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(), version), "Version field must be greater than 0");

    [Fact]
    public void NullFile_ReturnsTheConfiguredRequiredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(null!, null), "File cannot be empty!");

    [Fact]
    public void FileAndVersionFailuresUseKoalaValidationErrorCodes()
    {
        var invalidDtos = new[]
        {
            (new RodoUpsertDto(FormFile(length: 0), null), KoalaValidationErrorCodes.FileEmpty, EmptyFileMessage),
            (new RodoUpsertDto(FormFile(length: 2 * 1024 * 1024 + 1), null), KoalaValidationErrorCodes.FileToLarge, TooLargeMessage),
            (new RodoUpsertDto(FormFile(contentType: "text/plain"), null), KoalaValidationErrorCodes.FileInvalidType, ContentTypeMessage),
            (new RodoUpsertDto(FormFile(name: "document.gif"), null), KoalaValidationErrorCodes.FileInvalidType, ExtensionMessage),
            (new RodoUpsertDto(FormFile(), 0), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")
        };

        foreach (var (dto, code, message) in invalidDtos)
        {
            var result = _validator.Validate(dto);
            var matchingError = result.Errors.Single(error => error.ErrorMessage == message);
            Assert.Equal(code, matchingError.ErrorCode);
        }
    }

    public static IEnumerable<object[]> FileRuleAndVersionFailureCases
    {
        get
        {
            yield return new object[]
            {
                new RodoUpsertDto(FormFile(length: 0, name: "bad.gif", contentType: "text/plain"), 0),
                new[] { EmptyFileMessage, "Version field must be greater than 0" }
            };
            yield return new object[]
            {
                new RodoUpsertDto(FormFile(length: 2 * 1024 * 1024 + 1, name: "bad.gif", contentType: "text/plain"), -1),
                new[] { TooLargeMessage, "Version field must be greater than 0" }
            };
            yield return new object[]
            {
                new RodoUpsertDto(FormFile(name: "bad.gif", contentType: "text/plain"), null),
                new[] { ContentTypeMessage }
            };
        }
    }

    [Theory]
    [MemberData(nameof(FileRuleAndVersionFailureCases))]
    public void FileValidationStopsAtFirstRuleFailureWhileVersionIsStillChecked(RodoUpsertDto dto, string[] expected) =>
        ValidationAssertions.HasExactlyMessages(_validator, dto, expected);
}
