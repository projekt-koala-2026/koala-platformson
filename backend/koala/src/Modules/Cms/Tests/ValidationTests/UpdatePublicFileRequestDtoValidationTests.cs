using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class UpdatePublicFileRequestDtoValidationTests
{
    private readonly UpdatePublicFileRequestDtoValidator _validator = new();

    private static readonly string[] WhiteSpaceOnlyValues =
    [
        "", "\t", "\n", "\v", "\f", "\r", " ", "\u0085", "\u00A0", "\u1680",
        "\u2000", "\u2001", "\u2002", "\u2003", "\u2004", "\u2005", "\u2006", "\u2007",
        "\u2008", "\u2009", "\u200A", "\u2028", "\u2029", "\u202F", "\u205F", "\u3000",
        " \t"
    ];

    private static readonly string[] NonEmptyNames =
    [
        "A", "0", "Public file", "  padded name  ", "Zażółć gęślą jaźń", "東京", "Привет", "🙂",
        "file.pdf", "file/name", "[draft]", "https://example.test/a?b=c", "line\nbreak", "tab\tvalue",
        "quote \" mark", "backslash \\", "percent %", "<script>alert(1)</script>", "zero-width\u200Bchar",
        "combining e\u0301", "\u0000", "very-long-" + new string('x', 300), "-leading", "trailing-",
        "spaces and punctuation !?;"
    ];

    private static readonly string[] NonEmptyDescriptions =
    [
        "Summary", "0", "Multiple words are allowed", "  padded description  ", "Zażółć — opis", "東京の説明",
        "Приветствие", "emoji 🙂", "Reference: file.pdf", "a/b/c", "[draft]", "https://example.test/a?b=c",
        "line\nbreak", "tab\tvalue", "quote \" mark", "backslash \\", "percent %", "<p>description</p>",
        "zero-width\u200Bchar", "combining e\u0301", "\u0000", "long-" + new string('d', 300),
        "-leading", "trailing-", "special symbols !@#$%^&*()"
    ];

    public static IEnumerable<object[]> PublicFileMetadataCases
    {
        get
        {
            for (var i = 0; i < NonEmptyNames.Length; i++)
                yield return [NonEmptyNames[i], i % 2 == 0 ? null! : $"Optional description {i}", true, ""];

            foreach (var value in WhiteSpaceOnlyValues.Take(25))
                yield return [value, null!, false, KoalaValidationErrorCodes.PublicFileNameEmpty];

            for (var i = 0; i < NonEmptyDescriptions.Length; i++)
                yield return [$"Description case {i:D2}", NonEmptyDescriptions[i], true, ""];

            for (var i = 0; i < 25; i++)
                yield return [$"Whitespace description {i:D2}", WhiteSpaceOnlyValues[i], false, KoalaValidationErrorCodes.PublicFileDescriptionEmpty];
        }
    }

    [Theory]
    [MemberData(nameof(PublicFileMetadataCases))]
    public void MetadataDtoCoversRequiredNamesAndOptionalDescriptionSpace(string name, string? description, bool isValid, string errorCode)
    {
        var dto = new UpdatePublicFileRequestDto(name, description);
        if (isValid)
        {
            ValidationAssertions.IsValid(_validator, dto);
            return;
        }

        var message = errorCode == KoalaValidationErrorCodes.PublicFileNameEmpty
            ? "Name field cannot be empty!"
            : "Description field cannot be empty when not null!";
        ValidationAssertions.HasError(_validator, dto, errorCode, message);
    }
}
