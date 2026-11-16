using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class UpdateKoalicjantRequestDtoValidationTests
{
    private const string ValidContent = "{\"koalicjant_profile_image_url\":\"/profile.png\",\"body_pl\":\"Opis\",\"version\":\"1\"}";
    private static UpdateKoalicjantRequestDtoValidator Validator => new();
    private static UpdateKoalicjantRequestDto Model(string first = "Anna", string last = "Nowak", string email = "anna@example.test", string? content = null, int version = 1) => new(first, last, email, content ?? ValidContent, true, version);

    [Fact] public void AcceptsValidDto() => ValidationAssertions.IsValid(Validator, Model());
    [Theory]
    [InlineData("first", "", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData("first", " ", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData("first", "Anna1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("first", "Anna_", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("last", "", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData("last", " ", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData("last", "Nowak2", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    [InlineData("last", "Nowak.", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    public void ValidatesRequiredNameFields(string field, string value, string code, string message)
    {
        var dto = field == "first" ? Model(first: value) : Model(last: value);
        ValidationAssertions.HasError(Validator, dto, code, message);
    }
    [Theory]
    [InlineData("first", 1, false)] [InlineData("first", 49, false)] [InlineData("first", 50, false)] [InlineData("first", 51, true)]
    [InlineData("last", 1, false)] [InlineData("last", 49, false)] [InlineData("last", 50, false)] [InlineData("last", 51, true)]
    public void EnforcesNameLengthBoundaries(string field, int length, bool tooLong)
    {
        var value = new string('A', length);
        var dto = field == "first" ? Model(first: value) : Model(last: value);
        if (!tooLong) ValidationAssertions.IsValid(Validator, dto);
        else ValidationAssertions.HasError(Validator, dto, field == "first" ? KoalaValidationErrorCodes.NameFirstTooLong : KoalaValidationErrorCodes.NameLastTooLong,
            field == "first" ? "NameFirst field cannot be larger than 50 characters!" : "NameLast field cannot be larger than 50 characters!");
    }
    [Theory]
    [InlineData("A", "B")] [InlineData("Żółć", "Łódź")] [InlineData("Jean-Luc", "Van der Berg")]
    public void AcceptsUnicodeWhitespaceAndHyphensInNames(string first, string last) => ValidationAssertions.IsValid(Validator, Model(first, last));

    [Theory]
    [InlineData("")] [InlineData(" ")] [InlineData("\t")]
    public void RejectsEmptyEmail(string email) => ValidationAssertions.HasError(Validator, Model(email: email), KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!");
    [Theory]
    [InlineData("not-an-email")] [InlineData("@example.test")]
    public void RejectsMalformedEmail(string email) => ValidationAssertions.HasError(Validator, Model(email: email), KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!");
    [Theory]
    [InlineData("user@example.test")] [InlineData("first.last+tag@example.co.uk")] [InlineData("user_name@example.test")]
    public void AcceptsValidEmailFormats(string email) => ValidationAssertions.IsValid(Validator, Model(email: email));
    [Theory]
    [InlineData("a@b")] [InlineData("name @example.test")] [InlineData("name@example..test")] [InlineData("name@example.test.")]
    public void CharacterizesPermissiveEmailAddressRule(string email) => ValidationAssertions.IsValid(Validator, Model(email: email));
    [Fact]
    public void RejectsEmailLongerThan320Characters()
    {
        var email = new string('a', 310) + "@example.com";
        var result = Validator.Validate(Model(email: email));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.EmailTooLong && e.ErrorMessage == "Email field cannot be larger than 320 characters!");
    }
    [Theory] [InlineData(319, true)] [InlineData(320, true)] [InlineData(321, false)]
    public void ChecksEmailMaximumLengthBoundary(int length, bool valid)
    {
        const string suffix = "@example.com";
        var email = new string('a', length - suffix.Length) + suffix;
        var dto = Model(email: email);
        if (valid) ValidationAssertions.IsValid(Validator, dto);
        else ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!");
    }

    [Theory] [InlineData(1, true)] [InlineData(0, false)] [InlineData(-1, false)] [InlineData(int.MinValue, false)] [InlineData(int.MaxValue, true)]
    public void RequiresPositiveVersion(int value, bool valid)
    {
        var dto = Model(version: value);
        if (valid) ValidationAssertions.IsValid(Validator, dto);
        else ValidationAssertions.HasError(Validator, dto, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version cannot be 0 or less");
    }
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData("\t")]
    public void RejectsEmptyContentJson(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonEmpty, "ContentJson field cannot be empty!");
    [Theory] [InlineData("{")] [InlineData("null")] [InlineData("[]")] [InlineData("0")] [InlineData("\"text\"")]
    public void RejectsMalformedJsonAndNonObjectRoots(string json) => ValidationAssertions.HasError(Validator, Model(content: json), KoalaValidationErrorCodes.ContentJsonInvalid, "ContentJson field must be a valid json!");
    [Theory]
    [InlineData("koalicjant_profile_image_url", KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid)]
    [InlineData("body_pl", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RejectsMissingRequiredJsonProperty(string key, string code)
    {
        var fields = new Dictionary<string, string> { ["koalicjant_profile_image_url"] = "image", ["body_pl"] = "body", ["version"] = "1" };
        fields.Remove(key);
        var label = key == "koalicjant_profile_image_url" ? "profile_image_url" : key;
        ValidationAssertions.HasError(Validator, Model(content: System.Text.Json.JsonSerializer.Serialize(fields)), code, $"ContentJson filed must contain valid '{label}' field!");
    }
    [Theory]
    [InlineData("koalicjant_profile_image_url", "null", KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid)]
    [InlineData("koalicjant_profile_image_url", "1", KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid)]
    [InlineData("koalicjant_profile_image_url", "{}", KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid)]
    [InlineData("body_pl", "null", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)] [InlineData("body_pl", "[]", KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid)]
    [InlineData("version", "null", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)] [InlineData("version", "1", KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid)]
    public void RejectsNonStringJsonField(string key, string encoded, string code)
    {
        var json = $"{{\"koalicjant_profile_image_url\":\"image\",\"body_pl\":\"body\",\"version\":\"1\",\"{key}\":{encoded}}}";
        var label = key == "koalicjant_profile_image_url" ? "profile_image_url" : key;
        ValidationAssertions.HasError(Validator, Model(content: json), code, $"ContentJson filed must contain valid '{label}' field!");
    }
    [Theory]
    [InlineData("koalicjant_profile_image_url", "")] [InlineData("koalicjant_profile_image_url", " ")]
    [InlineData("body_pl", "")] [InlineData("body_pl", " \t")] [InlineData("version", "")] [InlineData("version", "\n")]
    public void RejectsBlankJsonString(string key, string value)
    {
        var fields = new Dictionary<string, string> { ["koalicjant_profile_image_url"] = "image", ["body_pl"] = "body", ["version"] = "1", [key] = value };
        var label = key == "koalicjant_profile_image_url" ? "profile_image_url" : key;
        var code = key == "koalicjant_profile_image_url" ? KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid : key == "body_pl" ? KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid : KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid;
        ValidationAssertions.HasError(Validator, Model(content: System.Text.Json.JsonSerializer.Serialize(fields)), code, $"ContentJson filed must contain valid '{label}' field!");
    }
    [Fact]
    public void ReportsIndependentErrorsTogether()
    {
        var result = Validator.Validate(Model(first: "", last: "", email: "bad", content: "{}", version: 0));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.NameFirstEmpty);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.NameLastEmpty);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.EmailInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ResourceVersionInvalid);
    }

    [Fact]
    public void MalformedJsonReturnsSyntaxAndRequiredPropertyFailures()
    {
        var result = Validator.Validate(Model(content: "{"));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonKoalicjantProfileImageUrlFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid);
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid);
    }
}
