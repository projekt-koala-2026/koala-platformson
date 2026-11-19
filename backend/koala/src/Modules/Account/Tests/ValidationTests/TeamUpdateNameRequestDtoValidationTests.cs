using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class TeamUpdateNameRequestDtoValidationTests
{
    private readonly TeamUpdateNameRequestDtoValidator _validator = new();
    public static IEnumerable<object[]> InvalidNameAndVersionCases
    {
        get
        {
            var names = new[]
            {
                ("", KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!"),
                (" ", KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!"),
                (new string('T', 51), KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters!")
            };
            var versions = new[] { 0, -1, int.MinValue };
            foreach (var name in names)
            foreach (var version in versions)
                yield return new object[]
                {
                    new TeamUpdateNameRequestDto(name.Item1, version),
                    new[] { (name.Item2, name.Item3), (KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0") }
                };
        }
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("T", 50)]
    public void NameBoundaryAndPositiveVersion_AreValid(string prefix, int length) =>
        ValidationAssertions.IsValid(_validator, new(prefix + new string('x', length - prefix.Length), 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new("Team", version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Fact]
    public void EmptyName_ReturnsTeamNameEmpty() =>
        ValidationAssertions.HasError(_validator, new("", 1), KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!");

    [Fact]
    public void WhitespaceName_ReturnsTeamNameEmpty() =>
        ValidationAssertions.HasError(_validator, new(" ", 1), KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!");

    [Fact]
    public void NameLength51_ReturnsTeamNameTooLong() =>
        ValidationAssertions.HasError(_validator, new(new string('T', 51), 1), KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters!");

    [Fact]
    public void VersionIntMax_IsAccepted() => ValidationAssertions.IsValid(_validator, new("Team", int.MaxValue));

    [Theory]
    [MemberData(nameof(InvalidNameAndVersionCases))]
    public void NameAndVersionFailuresAreBothReported(TeamUpdateNameRequestDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
