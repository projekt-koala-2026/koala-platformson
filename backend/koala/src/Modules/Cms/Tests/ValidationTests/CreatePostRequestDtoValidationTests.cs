using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class CreatePostRequestDtoValidationTests
{
    private const string ValidContent = "{\"title_pl\":\"Title\",\"body_pl\":\"Body\",\"version\":\"1\"}";
    private static CreatePostRequestDtoValidator Validator => new();
    private static CreatePostRequestDto Model(string name = "Post", Guid? edition = null, string? content = null) =>
        new(edition ?? Guid.NewGuid(), name, content ?? ValidContent, true);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory]
    [InlineData("A", false)] [InlineData("Post title", false)] [InlineData("\u017B\u00F3\u0142\u0107 i \u0141\u00F3d\u017A", false)]
    [InlineData("Name-With-Hyphen", false)] [InlineData("Name With Space", false)]
    [InlineData("", true)] [InlineData(" ", true)] [InlineData("\t", true)] [InlineData("\n", true)]
    [InlineData("Post1", false)] [InlineData("Post_One", false)] [InlineData("Post.Name", false)]
    [InlineData("Post!", false)] [InlineData("Post/Name", false)] [InlineData("\U0001F642", false)]
    public void ValidatesNameCharactersAndRequiredValue(string name, bool empty)
    {
        var dto = Model(name: name);
        if (empty) ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.PostNameEmpty, "NameFirst field cannot be empty!");
        else if (name.Any(char.IsDigit) || name.Contains('_') || name.Contains('.') || name.Contains('!') || name.Contains('/') || name.Contains("\U0001F642"))
            ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.PostNameInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!");
        else ValidationAssertions.IsValid(Validator, dto);
    }

    [Theory]
    [InlineData(1, false)] [InlineData(49, false)] [InlineData(50, false)] [InlineData(51, true)] [InlineData(100, true)]
    public void EnforcesNameLengthBoundary(int length, bool tooLong)
    {
        var dto = Model(name: new string('a', length));
        if (tooLong) ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.PostNameTooLong, "NameFirst field cannot be larger than 50 characters!");
        else ValidationAssertions.IsValid(Validator, dto);
    }

    [Fact] public void RejectsEmptyEditionId() => ValidationAssertions.HasError(Validator, Model(edition: Guid.Empty), KoalaValidationErrorCodes.TeamEditionIdEmpty, "EditionId field cannot be empty!");
    [Fact] public void RejectsInvalidEmptyGuidEditionId() => ValidationAssertions.HasError(Validator, Model(edition: Guid.Empty), KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID!");
    [Fact]
    public void EmptyEditionGuidReturnsBothConfiguredErrors()
    {
        ValidationAssertions.HasExactlyErrors(Validator, Model(edition: Guid.Empty),
            (KoalaValidationErrorCodes.TeamEditionIdEmpty, "EditionId field cannot be empty!"),
            (KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID!"));
    }
    [Theory]
    [InlineData("{\"title_pl\":\"T\",\"body_pl\":\"B\",\"version\":\"1\"}")]
    [InlineData("{\"title_pl\":\" T \",\"body_pl\":\" B \",\"version\":\" 1 \"}")]
    public void AcceptsValidJsonObjectAndNonEmptyStringFields(string json) => ValidationAssertions.IsValid(Validator, Model(content: json));

    [Fact]
    public void RequiresCaseSensitiveTitleFieldName() => ValidationAssertions.HasError(Validator, Model(content: "{\"TITLE_PL\":\"T\",\"body_pl\":\"B\",\"version\":\"1\"}"), KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "ContentJson filed must contain valid 'title_pl' field!");

    [Theory]
    [InlineData("")] [InlineData(" ")] [InlineData("\t\r\n")]
    public void RejectsEmptyContentJson(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");

    [Theory]
    [InlineData("{")] [InlineData("not-json")] [InlineData("null")] [InlineData("[]")]
    [InlineData("\"text\"")] [InlineData("42")] [InlineData("true")]
    public void RejectsInvalidJsonOrNonObjectRoot(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");

    [Theory]
    [InlineData("title_pl", "null")] [InlineData("title_pl", "123")] [InlineData("title_pl", "false")]
    [InlineData("title_pl", "[]")] [InlineData("title_pl", "{}")]
    [InlineData("body_pl", "null")] [InlineData("body_pl", "123")] [InlineData("body_pl", "[]")]
    [InlineData("version", "null")] [InlineData("version", "1")] [InlineData("version", "[]")]
    public void RequiresEachJsonFieldToBeANonEmptyString(string field, string encodedValue)
    {
        var json = $"{{\"title_pl\":\"T\",\"body_pl\":\"B\",\"version\":\"1\",\"{field}\":{encodedValue}}}";
        var code = field switch { "title_pl" => KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        var label = field switch { "title_pl" => "title_pl", "body_pl" => "body_pl", _ => "version" };
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{label}' field!");
    }

    [Theory]
    [InlineData("title_pl", "")] [InlineData("title_pl", " ")] [InlineData("body_pl", "")] [InlineData("body_pl", " \t")]
    [InlineData("version", "")] [InlineData("version", "\n")]
    public void RejectsBlankJsonStringFields(string field, string value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { ["title_pl"] = "T", ["body_pl"] = "B", ["version"] = "1", [field] = value });
        var code = field switch { "title_pl" => KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "body_pl" => KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, _ => KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid };
        var label = field;
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{label}' field!");
    }

    [Fact]
    public void ReportsAllRequiredJsonFieldsWhenObjectIsEmpty()
    {
        var result = Validator.Validate(Model(content: "{}"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid && e.ErrorMessage == "ContentJson filed must contain valid 'title_pl' field!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid && e.ErrorMessage == "ContentJson filed must contain valid 'body_pl' field!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid && e.ErrorMessage == "ContentJson filed must contain valid 'version' field!");
    }

    [Fact]
    public void MalformedJsonReturnsSyntaxAndAllRequiredFieldErrors()
    {
        var result = Validator.Validate(Model(content: "{"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonInvalid && e.ErrorMessage == "ContentJson field must be a valid json!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
    }
}
