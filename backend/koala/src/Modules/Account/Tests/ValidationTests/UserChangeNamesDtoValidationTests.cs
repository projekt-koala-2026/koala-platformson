using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserChangeNamesDtoValidationTests
{
    private readonly UserChangeNamesDtoValidator _validator = new();
    private static readonly (string Value, string Code, string Message)[] FirstFailures =
    {
        ("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!"),
        (new string('A', 51), KoalaValidationErrorCodes.NameFirstTooLong, "NameFirst field cannot be larger than 50 characters!"),
        ("Jan1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (string Value, string Code, string Message)[] LastFailures =
    {
        ("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!"),
        (new string('B', 51), KoalaValidationErrorCodes.NameLastTooLong, "NameLast field cannot be larger than 50 characters!"),
        ("Nowak_", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (int Value, string Code, string Message)[] VersionFailures =
    {
        (0, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0"),
        (-1, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0"),
        (int.MinValue, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")
    };

    public static IEnumerable<object[]> CombinedNameAndVersionCases
    {
        get
        {
            foreach (var first in FirstFailures)
            foreach (var last in LastFailures)
            foreach (var version in VersionFailures)
                yield return new object[]
                {
                    new UserChangeNamesDto(first.Value, last.Value, version.Value),
                    new[] { (first.Code, first.Message), (last.Code, last.Message), (version.Code, version.Message) }
                };
        }
    }

    [Fact]
    public void NamesOf50UnicodeLettersAndPositiveVersion_AreValid() =>
        ValidationAssertions.IsValid(_validator, new(new string('Ż', 50), new string('Ł', 50), 1));

    [Theory]
    [InlineData("Jan", "Nowak", 1)]
    [InlineData("Jean-Pierre", "Van Der Meer", 2)]
    [InlineData("Łucja", "Żółć", 25)]
    [InlineData("A B", "C-D", int.MaxValue)]
    public void DifferentAllowedNamesAndPositiveVersions_AreAccepted(string first, string last, int version) =>
        ValidationAssertions.IsValid(_validator, new(first, last, version));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData("Jan2", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Jan.", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    public void InvalidFirstName_ReturnsExpectedError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(name, "Nowak", 1), code, message);

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData("Nowak_", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    public void InvalidLastName_ReturnsExpectedError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, new("Jan", name, 1), code, message);

    [Fact]
    public void NamesLongerThan50_ReturnTheirTooLongErrors()
    {
        var result = _validator.Validate(new UserChangeNamesDto(new string('A', 51), new string('B', 51), 1));
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameFirstTooLong && error.ErrorMessage == "NameFirst field cannot be larger than 50 characters!");
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameLastTooLong && error.ErrorMessage == "NameLast field cannot be larger than 50 characters!");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new("Jan", "Nowak", version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Fact]
    public void VersionIntMax_IsAccepted() => ValidationAssertions.IsValid(_validator, new("Jan", "Nowak", int.MaxValue));

    [Theory]
    [MemberData(nameof(CombinedNameAndVersionCases))]
    public void AllNameAndVersionFailuresAreReportedTogether(UserChangeNamesDto dto, (string Code, string Message)[] expected)
    {
        var allExpected = expected.ToList();
        if (dto.NameFirst.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!"));
        if (dto.NameLast.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!"));
        ValidationAssertions.HasExactlyErrors(_validator, dto, allExpected.ToArray());
    }
}
