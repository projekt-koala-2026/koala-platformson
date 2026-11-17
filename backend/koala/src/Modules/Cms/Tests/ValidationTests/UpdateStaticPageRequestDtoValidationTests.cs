using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class UpdateStaticPageRequestDtoValidationTests
{
    private const string ValidContent = "{\"body_pl\":\"Treść\",\"version\":\"1\"}";
    private static UpdateStaticPageRequestDtoValidator Validator => new();
    private static UpdateStaticPageRequestDto Model(string? content = null, int version = 1) => new(content ?? ValidContent, version);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData("\t\r\n")]
    public void RejectsEmptyContentJson(string json) => ValidationAssertions.HasError(Validator, Model(json), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");
    [Theory]
    [InlineData("{")] [InlineData("not-json")] [InlineData("null")] [InlineData("[]")] [InlineData("\"text\"")] [InlineData("123")] [InlineData("false")]
    public void RejectsInvalidJsonOrNonObjectRoot(string json) => ValidationAssertions.HasError(Validator, Model(json), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");
    [Theory]
    [InlineData("{}")] [InlineData("{\"version\":\"1\"}")] [InlineData("{\"BODY_PL\":\"Body\",\"version\":\"1\"}")]
    public void RequiresBodyProperty(string json) => ValidationAssertions.HasError(Validator, Model(json), KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, "ContentJson filed must contain valid 'body_pl' field!");
    [Theory]
    [InlineData("{}")] [InlineData("{\"body_pl\":\"Body\"}")] [InlineData("{\"body_pl\":\"Body\",\"VERSION\":\"1\"}")]
    public void RequiresVersionJsonProperty(string json) => ValidationAssertions.HasError(Validator, Model(json), KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid, "ContentJson filed must contain valid 'version' field!");
    [Theory]
    [InlineData("body_pl", "null", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("body_pl", "1", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("body_pl", "{}", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("body_pl", "[]", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", "null", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    [InlineData("version", "1", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    [InlineData("version", "{}", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    [InlineData("version", "[]", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RequiresStringJsonValues(string key, string encoded, string code) =>
        ValidationAssertions.HasError(Validator, Model($"{{\"body_pl\":\"Body\",\"version\":\"1\",\"{key}\":{encoded}}}"), code, $"ContentJson filed must contain valid '{key}' field!");
    [Theory]
    [InlineData("body_pl", "")] [InlineData("body_pl", " ")] [InlineData("body_pl", "\t\n")]
    [InlineData("version", "")] [InlineData("version", " ")] [InlineData("version", "\t\n")]
    public void RejectsWhitespaceOnlyJsonStrings(string key, string value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { ["body_pl"] = "Body", ["version"] = "1", [key] = value });
        ValidationAssertions.HasError(Validator, Model(json), key == "body_pl" ? KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid : KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid, $"ContentJson filed must contain valid '{key}' field!");
    }
    [Theory]
    [InlineData("{\"body_pl\":\"First\",\"body_pl\":\"Last\",\"version\":\"1\"}")]
    [InlineData("{\"body_pl\":\"<p>Łódź</p>\",\"version\":\"semver:1.0\"}")]
    [InlineData("{\"body_pl\":\"\\u0000\",\"version\":\"\\u0000\"}")]
    public void AcceptsAnyNonWhitespaceStringValues(string json) => ValidationAssertions.IsValid(Validator, Model(json));
    [Theory] [InlineData(1, true)] [InlineData(0, false)] [InlineData(-1, false)] [InlineData(int.MinValue, false)] [InlineData(int.MaxValue, true)]
    public void RequiresPositiveResourceVersion(int value, bool valid)
    {
        var dto = Model(version: value);
        if (valid) ValidationAssertions.IsValid(Validator, dto);
        else ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version cannot be 0 or less");
    }
    [Fact]
    public void ReturnsJsonAndModelVersionErrorsTogether()
    {
        var result = Validator.Validate(Model("{}", 0));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid && e.ErrorMessage == "ContentJson filed must contain valid 'body_pl' field!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid && e.ErrorMessage == "ContentJson filed must contain valid 'version' field!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ResourceVersionInvalid && e.ErrorMessage == "Version cannot be 0 or less");
    }

    [Fact]
    public void MalformedJsonReturnsSyntaxAndRequiredPropertyFailures()
    {
        var result = Validator.Validate(Model("{"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
    }
}
