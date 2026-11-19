using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class UpdateSponsorRequestDtoValidationTests
{
    private const string ValidContent = "{\"sponsor_web_url\":\"https://example.test\",\"sponsor_logo_image_url\":\"/logo.svg\",\"body_pl\":\"Opis\",\"version\":\"1\"}";
    private static UpdateSponsorRequestDtoValidator Validator => new();
    private static UpdateSponsorRequestDto Model(string name = "Sponsor", string? content = null, int version = 1) => new(name, content ?? ValidContent, true, version);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory]
    [InlineData("", KoalaValidationErrorCodes.SponsorNameEmpty, "NameFirst field cannot be empty!")]
    [InlineData("  ", KoalaValidationErrorCodes.SponsorNameEmpty, "NameFirst field cannot be empty!")]
    [InlineData("Sponsor1", KoalaValidationErrorCodes.SponsorNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Sponsor_One", KoalaValidationErrorCodes.SponsorNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Sponsor!", KoalaValidationErrorCodes.SponsorNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Łódź - Kraków", "", "")]
    public void ValidatesSponsorName(string name, string code, string message)
    {
        if (code.Length == 0) ValidationAssertions.IsValid(Validator, Model(name));
        else ValidationAssertions.HasError(Validator, Model(name), code, message);
    }
    [Theory] [InlineData(1, false)] [InlineData(49, false)] [InlineData(50, false)] [InlineData(51, true)] [InlineData(300, true)]
    public void EnforcesNameLength(int length, bool invalid)
    {
        var dto = Model(new string('S', length));
        if (invalid) ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.SponsorNameTooLong, "NameFirst field cannot be larger than 50 characters!");
        else ValidationAssertions.IsValid(Validator, dto);
    }
    [Theory] [InlineData(1, true)] [InlineData(0, false)] [InlineData(-1, false)] [InlineData(int.MinValue, false)] [InlineData(int.MaxValue, true)]
    public void RequiresPositiveVersion(int value, bool valid)
    {
        var dto = Model(version: value);
        if (valid) ValidationAssertions.IsValid(Validator, dto);
        else ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version cannot be 0 or less");
    }
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData("\t")]
    public void RejectsEmptyJson(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");
    [Theory] [InlineData("{")] [InlineData("invalid")] [InlineData("null")] [InlineData("[]")] [InlineData("1")] [InlineData("\"text\"")]
    public void RejectsInvalidJson(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");
    [Theory]
    [InlineData("sponsor_web_url", "sponsor_web_url", KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid)]
    [InlineData("sponsor_logo_image_url", "sponsor_logo_image_url", KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid)]
    [InlineData("body_pl", "body_pl", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", "version", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RejectsMissingJsonProperty(string key, string label, string code)
    {
        var fields = new Dictionary<string, string> { ["sponsor_web_url"] = "url", ["sponsor_logo_image_url"] = "logo", ["body_pl"] = "body", ["version"] = "1" };
        fields.Remove(key);
        ValidationAssertions.HasError(Validator, Model(content: System.Text.Json.JsonSerializer.Serialize(fields)), code, $"ContentJson filed must contain valid '{label}' field!");
    }
    [Theory]
    [InlineData("sponsor_web_url", "null")] [InlineData("sponsor_web_url", "1")] [InlineData("sponsor_web_url", "{}")] [InlineData("sponsor_web_url", "[]")]
    [InlineData("sponsor_logo_image_url", "null")] [InlineData("sponsor_logo_image_url", "1")] [InlineData("sponsor_logo_image_url", "{}")] [InlineData("sponsor_logo_image_url", "[]")]
    [InlineData("body_pl", "null")] [InlineData("body_pl", "1")] [InlineData("body_pl", "{}")] [InlineData("body_pl", "[]")]
    [InlineData("version", "null")] [InlineData("version", "1")] [InlineData("version", "{}")] [InlineData("version", "[]")]
    public void RequiresStringJsonValues(string key, string encoded)
    {
        var json = $"{{\"sponsor_web_url\":\"url\",\"sponsor_logo_image_url\":\"logo\",\"body_pl\":\"body\",\"version\":\"1\",\"{key}\":{encoded}}}";
        var code = key switch { "sponsor_web_url" => KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid, "sponsor_logo_image_url" => KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{key}' field!");
    }
    [Theory]
    [InlineData("sponsor_web_url", "")] [InlineData("sponsor_web_url", " ")] [InlineData("sponsor_logo_image_url", "")] [InlineData("sponsor_logo_image_url", " ")]
    [InlineData("body_pl", "")] [InlineData("body_pl", " ")] [InlineData("version", "")] [InlineData("version", " ")]
    public void RejectsBlankJsonString(string key, string value)
    {
        var fields = new Dictionary<string, string> { ["sponsor_web_url"] = "url", ["sponsor_logo_image_url"] = "logo", ["body_pl"] = "body", ["version"] = "1", [key] = value };
        var code = key switch { "sponsor_web_url" => KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid, "sponsor_logo_image_url" => KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: System.Text.Json.JsonSerializer.Serialize(fields)), code, $"ContentJson filed must contain valid '{key}' field!");
    }
    [Fact]
    public void ReportsJsonAndVersionErrorsTogether()
    {
        var result = Validator.Validate(Model(content: "{}", version: 0));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ResourceVersionInvalid);
    }

    [Fact]
    public void MalformedJsonReturnsSyntaxAndRequiredPropertyFailures()
    {
        var result = Validator.Validate(Model(content: "{"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
    }
}
