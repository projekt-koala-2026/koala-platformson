using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class TeamCreateRequestDtoValidationTests
{
    private readonly TeamCreateRequestDtoValidator _validator = new();
    public static IEnumerable<object[]> SimultaneousNameAndSchoolIdFailures
    {
        get
        {
            var names = new[]
            {
                ("", KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!"),
                (" ", KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!"),
                (new string('T', 51), KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters!")
            };
            foreach (var name in names)
                yield return new object[]
                {
                    new TeamCreateRequestDto(name.Item1, Guid.Empty),
                    new[]
                    {
                        (name.Item2, name.Item3),
                        (KoalaValidationErrorCodes.TeamSchoolIdEmpty, "SchoolId field cannot be empty!"),
                        (KoalaValidationErrorCodes.TeamSchoolIdInvalid, "SchoolId field must be a valid Guid7 ID!")
                    }
                };
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void NameLengthAtOrBelow50_IsAccepted(int length) =>
        ValidationAssertions.IsValid(_validator, new(new string('x', length), Guid.NewGuid()));

    [Fact]
    public void EmptyName_ReturnsTeamNameEmpty() =>
        ValidationAssertions.HasError(_validator, new("", Guid.NewGuid()), KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!");

    [Fact]
    public void WhitespaceName_ReturnsTeamNameEmpty() =>
        ValidationAssertions.HasError(_validator, new(" ", Guid.NewGuid()), KoalaValidationErrorCodes.TeamNameEmpty, "Name field cannot be empty!");

    [Fact]
    public void NameLength51_ReturnsTeamNameTooLong() =>
        ValidationAssertions.HasError(_validator, new(new string('x', 51), Guid.NewGuid()), KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters!");

    [Theory]
    [InlineData(KoalaValidationErrorCodes.TeamSchoolIdEmpty, "SchoolId field cannot be empty!")]
    [InlineData(KoalaValidationErrorCodes.TeamSchoolIdInvalid, "SchoolId field must be a valid Guid7 ID!")]
    public void EmptySchoolId_ReturnsBothConfiguredErrors(string code, string message) =>
        ValidationAssertions.HasError(_validator, new("Team", Guid.Empty), code, message);

    [Fact]
    public void NonEmptySchoolId_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new("Team", Guid.NewGuid()));

    [Theory]
    [MemberData(nameof(SimultaneousNameAndSchoolIdFailures))]
    public void AllNameAndSchoolIdErrorsAreReturned(TeamCreateRequestDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
