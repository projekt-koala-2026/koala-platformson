using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class CreateSponsorRequestDtoValidationTests
{
    private const string ValidContent = "{\"sponsor_web_url\":\"https://example.test\",\"sponsor_logo_image_url\":\"/logo.svg\",\"body_pl\":\"Opis\",\"version\":\"1\"}";
    private static CreateSponsorRequestDtoValidator Validator => new();
    private static CreateSponsorRequestDto Model(string name = "Sponsor", string? content = null) => new(name, content ?? ValidContent, true);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory]
    [InlineData("", KoalaValidationErrorCodes.SponsorNameEmpty)] [InlineData(" ", KoalaValidationErrorCodes.SponsorNameEmpty)]
    [InlineData("1 Sponsor", KoalaValidationErrorCodes.SponsorNameInvalidCharacter)] [InlineData("Sponsor_One", KoalaValidationErrorCodes.SponsorNameInvalidCharacter)]
    [InlineData("Sponsor!", KoalaValidationErrorCodes.SponsorNameInvalidCharacter)] [InlineData("Sponsor/One", KoalaValidationErrorCodes.SponsorNameInvalidCharacter)]
    public void ValidatesName(string name, string code)
    {
        var message = code == KoalaValidationErrorCodes.SponsorNameEmpty ? "NameFirst field cannot be empty!" : "NameFirst field can only contain letters, spaces, or hyphens!";
        ValidationAssertions.HasError(Validator, Model(name), code, message);
    }
    [Theory] [InlineData(1, false)] [InlineData(49, false)] [InlineData(50, false)] [InlineData(51, true)] [InlineData(200, true)]
    public void EnforcesNameLengthBoundary(int length, bool invalid)
    {
        var dto = Model(new string('S', length));
        if (invalid) ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.SponsorNameTooLong, "NameFirst field cannot be larger than 50 characters!");
        else ValidationAssertions.IsValid(Validator, dto);
    }
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData("\t\n")]
    public void RejectsEmptyJson(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");
    [Theory] [InlineData("{")] [InlineData("nope")] [InlineData("null")] [InlineData("[]")] [InlineData("42")] [InlineData("\"string\"")]
    public void RejectsMalformedJsonAndNonObjectRoot(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");
    [Theory]
    [InlineData("sponsor_web_url", "sponsor_web_url", KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid)]
    [InlineData("sponsor_logo_image_url", "sponsor_logo_image_url", KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid)]
    [InlineData("body_pl", "body_pl", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", "version", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RejectsMissingRequiredJsonProperty(string key, string label, string code)
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
    public void RequiresJsonPropertyToBeAString(string key, string encoded)
    {
        var json = $"{{\"sponsor_web_url\":\"url\",\"sponsor_logo_image_url\":\"logo\",\"body_pl\":\"body\",\"version\":\"1\",\"{key}\":{encoded}}}";
        var code = key switch { "sponsor_web_url" => KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid, "sponsor_logo_image_url" => KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{key}' field!");
    }
    [Theory]
    [InlineData("sponsor_web_url", "")] [InlineData("sponsor_web_url", " ")] [InlineData("sponsor_logo_image_url", "")] [InlineData("sponsor_logo_image_url", " ")]
    [InlineData("body_pl", "")] [InlineData("body_pl", " ")] [InlineData("version", "")] [InlineData("version", " ")]
    public void RejectsWhitespaceOnlyJsonStrings(string key, string value)
    {
        var fields = new Dictionary<string, string> { ["sponsor_web_url"] = "url", ["sponsor_logo_image_url"] = "logo", ["body_pl"] = "body", ["version"] = "1", [key] = value };
        var code = key switch { "sponsor_web_url" => KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid, "sponsor_logo_image_url" => KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: System.Text.Json.JsonSerializer.Serialize(fields)), code, $"ContentJson filed must contain valid '{key}' field!");
    }
    [Fact]
    public void ReportsAllJsonFieldErrorsAtOnce()
    {
        var result = Validator.Validate(Model(content: "{}"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorWebUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonSponsorLogoImageUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
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
