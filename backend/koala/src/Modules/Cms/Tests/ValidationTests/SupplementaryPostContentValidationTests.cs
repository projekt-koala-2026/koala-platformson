using System.Text.Json;
using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class SupplementaryPostContentValidationTests
{
    private static readonly CreatePostRequestDtoValidator Validator = new();
    private static readonly string[] RequiredFields = ["title_pl", "body_pl", "version"];

    private static readonly string[] WhitespaceOnlyValues =
    [
        "\t", "\n", "\v", "\f", "\r", " ", "\u0085", "\u00A0", "\u1680", "\u2000",
        "\u2001", "\u2002", "\u2003", "\u2004", "\u2005", "\u2006", "\u2007", "\u2008",
        "\u2009", "\u200A"
    ];

    private static readonly string[] NonEmptyValues =
    [
        "0", "null", "true", "{}", "[]", "Title", "  padded  ", "Zażółć gęślą jaźń", "東京",
        "Привет", "مرحبا", "नमस्ते", "🙂", "emoji 👩‍💻", "<p>HTML</p>", "line\nbreak",
        "tab\tvalue", "quote \" mark", "backslash \\", "slash /", "comma,value", "colon:value",
        "semicolon;value", "apostrophe ' value", "&amp; entity", "<script>alert(1)</script>",
        "zero-width\u200Bchar", "combining e\u0301", "\u0000", "\u001F", "http://example.test/a?b=c",
        "v1.0.0", "-leading-hyphen", "trailing-space ", "value\r\nnext", "brackets [x]",
        "backtick ` value", "math + - * /", "percent % value", "very-long-" + new string('x', 200)
    ];

    public static IEnumerable<object[]> RequiredFieldValueCases
    {
        get
        {
            foreach (var requiredField in RequiredFields)
            foreach (var value in WhitespaceOnlyValues)
                yield return [requiredField, value, false];

            for (var i = 0; i < NonEmptyValues.Length; i++)
                yield return [RequiredFields[i % RequiredFields.Length], NonEmptyValues[i], true];
        }
    }

    [Theory]
    [MemberData(nameof(RequiredFieldValueCases))]
    public void RequiredJsonStringFieldsHandleTwentyWhitespaceAndFortyContentValues(string requiredField, string value, bool shouldBeValid)
    {
        var content = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["title_pl"] = requiredField == "title_pl" ? value : "Valid title",
            ["body_pl"] = requiredField == "body_pl" ? value : "Valid body",
            ["version"] = requiredField == "version" ? value : "1"
        });
        var dto = new CreatePostRequestDto(Guid.NewGuid(), "Valid post", content, true);

        if (shouldBeValid)
        {
            ValidationAssertions.IsValid(Validator, dto);
            return;
        }

        var (code, label) = requiredField switch
        {
            "title_pl" => (KoalaValidationErrorCodes.ContentJsonTitlePLFieldInvalid, "title_pl"),
            "body_pl" => (KoalaValidationErrorCodes.ContentJsonBodyPLFieldInvalid, "body_pl"),
            _ => (KoalaValidationErrorCodes.ContentJsonVersionFieldInvalid, "version")
        };
        ValidationAssertions.HasError(Validator, dto, code, $"ContentJson filed must contain valid '{label}' field!");
    }
}
