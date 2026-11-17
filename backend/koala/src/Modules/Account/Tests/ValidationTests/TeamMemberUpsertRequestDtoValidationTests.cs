using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class TeamMemberUpsertRequestDtoValidationTests
{
    private readonly TeamMemberUpsertRequestDtoValidator _validator = new();
    private static readonly (string Value, string Code, string Message)[] NameFailuresFirst =
    {
        ("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!"),
        (new string('A', 51), KoalaValidationErrorCodes.NameFirstTooLong, "NameFirst field cannot be larger than 50 characters!"),
        ("Jan1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (string Value, string Code, string Message)[] NameFailuresLast =
    {
        ("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!"),
        (new string('B', 51), KoalaValidationErrorCodes.NameLastTooLong, "NameLast field cannot be larger than 50 characters!"),
        ("Nowak_", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (int Value, string Code, string Message)[] InvalidVersions =
    {
        (0, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0"),
        (-1, KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")
    };

    public static IEnumerable<object[]> CombinedMemberAndVersionCases
    {
        get
        {
            foreach (var first in NameFailuresFirst)
            foreach (var last in NameFailuresLast)
            foreach (var version in InvalidVersions)
                yield return new object[]
                {
                    new TeamMemberUpsertRequestDto(first.Value, last.Value, version.Value),
                    new[] { (first.Code, first.Message), (last.Code, last.Message), (version.Code, version.Message) }
                };
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void NamesAtMaximumAndPositiveOrNullVersion_AreValid(int length) =>
        ValidationAssertions.IsValid(_validator, new(new string('A', length), new string('Ł', length), 1));

    [Fact]
    public void NullOptionalVersion_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new("Jan", "Nowak", null));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData("Jan1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    public void InvalidFirstName_ReturnsExpectedError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(name, "Nowak", null), code, message);

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData("Nowak_", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    public void InvalidLastName_ReturnsExpectedError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, new("Jan", name, null), code, message);

    [Fact]
    public void NamesOver50Characters_ReturnTheirTooLongCodes()
    {
        var result = _validator.Validate(new TeamMemberUpsertRequestDto(new string('A', 51), new string('B', 51), null));
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameFirstTooLong && error.ErrorMessage == "NameFirst field cannot be larger than 50 characters!");
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameLastTooLong && error.ErrorMessage == "NameLast field cannot be larger than 50 characters!");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveOptionalVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new("Jan", "Nowak", version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Theory]
    [MemberData(nameof(CombinedMemberAndVersionCases))]
    public void AllMemberNameAndVersionFailuresAreReportedTogether(TeamMemberUpsertRequestDto dto, (string Code, string Message)[] expected)
    {
        var allExpected = expected.ToList();
        if (dto.NameFirst.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!"));
        if (dto.NameLast.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!"));
        ValidationAssertions.HasExactlyErrors(_validator, dto, allExpected.ToArray());
    }
}
