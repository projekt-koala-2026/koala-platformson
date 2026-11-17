using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class UpdatePostRequestDtoValidationTests
{
    private const string ValidContent = "{\"title_pl\":\"Tytuł\",\"body_pl\":\"Treść\",\"version\":\"1\"}";
    private static UpdatePostRequestDtoValidator Validator => new();
    private static UpdatePostRequestDto Model(string name = "Post", Guid? edition = null, string? content = null, int version = 1) => new(edition ?? Guid.NewGuid(), name, content ?? ValidContent, true, version);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory]
    [InlineData("", KoalaValidationErrorCodes.PostNameEmpty, "NameFirst field cannot be empty!")]
    [InlineData("  ", KoalaValidationErrorCodes.PostNameEmpty, "NameFirst field cannot be empty!")]
    [InlineData("Name1", KoalaValidationErrorCodes.PostNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Name_One", KoalaValidationErrorCodes.PostNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Name!", KoalaValidationErrorCodes.PostNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Żółć-Łódź", "", "")]
    public void ValidatesName(string name, string code, string message)
    {
        if (code.Length == 0) ValidationAssertions.IsValid(Validator, Model(name: name));
        else ValidationAssertions.HasError(Validator, Model(name: name), code, message);
    }
    [Theory]
    [InlineData(1, false)] [InlineData(49, false)] [InlineData(50, false)] [InlineData(51, true)] [InlineData(320, true)]
    public void EnforcesNameLength(int length, bool tooLong)
    {
        var dto = Model(name: new string('A', length));
        if (tooLong) ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.PostNameTooLong, "NameFirst field cannot be larger than 50 characters!");
        else ValidationAssertions.IsValid(Validator, dto);
    }
    [Fact] public void RejectsEmptyEditionId() => ValidationAssertions.HasError(Validator, Model(edition: Guid.Empty), KoalaValidationErrorCodes.TeamEditionIdEmpty, "EditionId field cannot be empty!");
    [Fact] public void RejectsInvalidEditionId() => ValidationAssertions.HasError(Validator, Model(edition: Guid.Empty), KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID!");
    [Theory]
    [InlineData("")] [InlineData(" ")] [InlineData("\t")]
    public void RejectsEmptyJson(string content) => ValidationAssertions.HasError(Validator, Model(content: content), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");
    [Theory]
    [InlineData("{")] [InlineData("null")] [InlineData("[]")] [InlineData("\"value\"")] [InlineData("0")]
    public void RejectsMalformedJsonAndNonObjectRoots(string content) => ValidationAssertions.HasError(Validator, Model(content: content), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");
    [Theory]
    [InlineData("title_pl", "title_pl", KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid)]
    [InlineData("body_pl", "body_pl", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", "version", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RejectsMissingJsonField(string field, string label, string code)
    {
        var json = field switch { "title_pl" => "{\"body_pl\":\"Body\",\"version\":\"1\"}", "body_pl" => "{\"title_pl\":\"Title\",\"version\":\"1\"}", _ => "{\"title_pl\":\"Title\",\"body_pl\":\"Body\"}" };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{label}' field!");
    }
    [Theory]
    [InlineData("title_pl", "null")] [InlineData("title_pl", "0")] [InlineData("title_pl", "{}")] [InlineData("title_pl", "[]")]
    [InlineData("body_pl", "null")] [InlineData("body_pl", "0")] [InlineData("body_pl", "{}")] [InlineData("body_pl", "[]")]
    [InlineData("version", "null")] [InlineData("version", "0")] [InlineData("version", "{}")] [InlineData("version", "[]")]
    public void RejectsNonStringJsonField(string field, string value)
    {
        var json = $"{{\"title_pl\":\"T\",\"body_pl\":\"B\",\"version\":\"1\",\"{field}\":{value}}}";
        var code = field switch { "title_pl" => KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{field}' field!");
    }
    [Theory]
    [InlineData("title_pl", "")] [InlineData("title_pl", " ")] [InlineData("body_pl", "")] [InlineData("body_pl", " \t")]
    [InlineData("version", "")] [InlineData("version", "\n")]
    public void RejectsWhitespaceJsonField(string field, string value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { ["title_pl"] = "T", ["body_pl"] = "B", ["version"] = "1", [field] = value });
        var code = field switch { "title_pl" => KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{field}' field!");
    }
    [Theory]
    [InlineData(1, true)] [InlineData(0, false)] [InlineData(-1, false)] [InlineData(int.MinValue, false)] [InlineData(int.MaxValue, true)]
    public void RequiresPositiveVersion(int version, bool valid) { var dto = Model(version: version); if (valid) ValidationAssertions.IsValid(Validator, dto); else ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version cannot be 0 or less"); }
    [Fact]
    public void ReportsMultipleIndependentFailuresTogether()
    {
        var result = Validator.Validate(Model(name: "", edition: Guid.Empty, content: "{}", version: 0));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.PostNameEmpty);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.TeamEditionIdEmpty);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ResourceVersionInvalid);
    }

    [Fact]
    public void MalformedJsonReturnsSyntaxAndRequiredPropertyFailures()
    {
        var result = Validator.Validate(Model(content: "{"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
    }
}
