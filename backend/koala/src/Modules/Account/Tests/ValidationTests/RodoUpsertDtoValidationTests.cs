using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class RodoUpsertDtoValidationTests
{
    private const string EmptyFileMessage = "File cannot be empty!";
    private const string TooLargeMessage = "File size cannot exceed 4 MB!";
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
    public void MaximumFileSizeOf4MiB_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new(FormFile(length: 4 * 1024 * 1024), 1));

    [Fact]
    public void EmptyFile_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(length: 0), null), EmptyFileMessage);

    [Fact]
    public void FileOneByteOver4MiB_ReturnsConfiguredMessage() =>
        ValidationAssertions.HasMessage(_validator, new(FormFile(length: 4 * 1024 * 1024 + 1), null), TooLargeMessage);

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
            (new RodoUpsertDto(FormFile(length: 4 * 1024 * 1024 + 1), null), KoalaValidationErrorCodes.FileToLarge, TooLargeMessage),
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
                new RodoUpsertDto(FormFile(length: 4 * 1024 * 1024 + 1, name: "bad.gif", contentType: "text/plain"), -1),
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

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("application/pdf")]
    [InlineData("IMAGE/JPEG")]
    [InlineData("IMAGE/PNG")]
    [InlineData("IMAGE/WEBP")]
    [InlineData("APPLICATION/PDF")]
    public void EveryAllowedContentTypeIsAcceptedRegardlessOfLetterCase(string contentType) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(contentType: contentType), null));

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/csv")]
    [InlineData("application/octet-stream")]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]
    [InlineData("image/bmp")]
    [InlineData("image/tiff")]
    [InlineData("application/json")]
    [InlineData("application/zip")]
    [InlineData("application/msword")]
    [InlineData("application/xml")]
    [InlineData("video/mp4")]
    [InlineData("audio/mpeg")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown/type")]
    public void RejectsEveryUnsupportedContentType(string contentType) =>
        ValidationAssertions.HasError(_validator, new(FormFile(contentType: contentType), null), KoalaValidationErrorCodes.FileInvalidType, ContentTypeMessage);

    [Theory]
    [InlineData("photo.gif")]
    [InlineData("photo.bmp")]
    [InlineData("photo.tif")]
    [InlineData("photo.tiff")]
    [InlineData("photo.svg")]
    [InlineData("photo.heic")]
    [InlineData("photo.jfif")]
    [InlineData("photo.zip")]
    [InlineData("photo.exe")]
    [InlineData("photo.docx")]
    [InlineData("photo.png.exe")]
    [InlineData("photo.jpg.exe")]
    [InlineData("photo.pdf.exe")]
    [InlineData("photo")]
    [InlineData("photo.")]
    [InlineData("photo.pdf ")]
    public void RejectsEveryUnsupportedExtension(string fileName) =>
        ValidationAssertions.HasError(_validator, new(FormFile(name: fileName), null), KoalaValidationErrorCodes.FileInvalidType, ExtensionMessage);

    [Theory]
    [InlineData("image/jpeg", "photo.jpg")]
    [InlineData("image/jpeg", "photo.jpeg")]
    [InlineData("image/png", "photo.png")]
    [InlineData("image/webp", "photo.webp")]
    [InlineData("application/pdf", "document.pdf")]
    [InlineData("IMAGE/JPEG", "PHOTO.JPEG")]
    [InlineData("IMAGE/PNG", "PHOTO.PNG")]
    [InlineData("IMAGE/WEBP", "PHOTO.WEBP")]
    [InlineData("APPLICATION/PDF", "DOCUMENT.PDF")]
    public void AcceptsEveryAllowedExtensionIncludingUppercase(string contentType, string fileName) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(name: fileName, contentType: contentType), null));

    [Theory]
    [InlineData(1)]
    [InlineData(4 * 1024 * 1024 - 1)]
    [InlineData(4 * 1024 * 1024)]
    public void AcceptsPositiveFilesAtOrBelowFourMiB(int length) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(length: length), null));

    [Theory]
    [InlineData(4 * 1024 * 1024 + 1)]
    [InlineData(4 * 1024 * 1024 + 1024)]
    [InlineData(5 * 1024 * 1024)]
    public void RejectsFilesAboveFourMiB(int length) =>
        ValidationAssertions.HasError(_validator, new(FormFile(length: length), null), KoalaValidationErrorCodes.FileToLarge, TooLargeMessage);

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-100)]
    [InlineData(-2)]
    [InlineData(0)]
    public void EveryNonPositiveVersionIsRejectedWithTheConfiguredCode(int version) =>
        ValidationAssertions.HasError(_validator, new(FormFile(), version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void EveryPositiveVersionIsAccepted(int version) =>
        ValidationAssertions.IsValid(_validator, new(FormFile(), version));

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void NullFileStopsFileRulesButStillValidatesVersion(int? version)
    {
        var result = _validator.Validate(new RodoUpsertDto(null!, version));
        var expected = version is > 0
            ? new[] { EmptyFileMessage }
            : version.HasValue ? new[] { EmptyFileMessage, "Version field must be greater than 0" } : new[] { EmptyFileMessage };
        ValidationAssertions.HasExactlyMessages(_validator, new RodoUpsertDto(null!, version), expected);
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.FileEmpty);
        if (version is <= 0)
            Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.ResourceVersionInvalid);
    }

    [Theory]
    [InlineData("application/pdf", "photo.gif", 0, EmptyFileMessage)]
    [InlineData("text/plain", "photo.gif", 1, ContentTypeMessage)]
    [InlineData("application/pdf", "photo.gif", 1, ExtensionMessage)]
    [InlineData("image/png", "photo.png", 4 * 1024 * 1024 + 1, TooLargeMessage)]
    public void FileValidationStopsAtTheFirstFailure(string contentType, string name, int length, string expectedMessage)
    {
        var dto = new RodoUpsertDto(FormFile(length, name, contentType), null);
        var result = _validator.Validate(dto);
        Assert.Equal(expectedMessage, Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public void EachFileFailureHasItsCorrespondingKoalaErrorCode()
    {
        var cases = new[]
        {
            (FormFile(length: 0), KoalaValidationErrorCodes.FileEmpty, EmptyFileMessage),
            (FormFile(length: 4 * 1024 * 1024 + 1), KoalaValidationErrorCodes.FileToLarge, TooLargeMessage),
            (FormFile(contentType: "text/plain"), KoalaValidationErrorCodes.FileInvalidType, ContentTypeMessage),
            (FormFile(name: "invalid.gif"), KoalaValidationErrorCodes.FileInvalidType, ExtensionMessage)
        };

        foreach (var (file, code, message) in cases)
        {
            var failure = Assert.Single(_validator.Validate(new RodoUpsertDto(file, null)).Errors);
            Assert.Equal(code, failure.ErrorCode);
            Assert.Equal(message, failure.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("application/pdf; charset=binary")]
    [InlineData("image/jpeg; charset=utf-8")]
    [InlineData("image/png; charset=utf-8")]
    [InlineData("application/pdf ")]
    public void RejectsContentTypesWithParametersOrTrailingWhitespace(string contentType) =>
        ValidationAssertions.HasError(_validator, new(FormFile(contentType: contentType), null), KoalaValidationErrorCodes.FileInvalidType, ContentTypeMessage);

    [Theory]
    [InlineData("photo.pdf?download=1")]
    [InlineData("photo.pdf#fragment")]
    [InlineData("photo.jpeg.exe")]
    public void RejectsFilenamesWhoseFinalExtensionIsNotAnAllowedSuffix(string fileName) =>
        ValidationAssertions.HasError(_validator, new(FormFile(name: fileName), null), KoalaValidationErrorCodes.FileInvalidType, ExtensionMessage);

    [Fact]
    public void DotfileNamedPdfIsAcceptedBecauseItsSuffixMatchesAnAllowedExtension() =>
        ValidationAssertions.IsValid(_validator, new(FormFile(name: ".pdf"), null));
}
