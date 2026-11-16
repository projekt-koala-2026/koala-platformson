using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class TeamQueryDtoValidationTests
{
    private readonly TeamQueryDtoValidator _validator = new();
    private static TeamQueryDto Query(Guid? edition = null, Guid? school = null, Guid? captain = null, Guid? guardian = null, string? name = null) => new(edition, school, captain, guardian, name);

    private static readonly (string Field, string Code, string Message)[] InvalidFilters =
    {
        ("edition", KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID if provided!"),
        ("school", KoalaValidationErrorCodes.TeamSchoolIdInvalid, "SchoolId field must be a valid Guid7 ID if provided!"),
        ("captain", KoalaValidationErrorCodes.TeamCaptainIdInvalid, "CaptainId field must be a valid Guid7 ID if provided!"),
        ("guardian", KoalaValidationErrorCodes.TeamGuardianIdInvalid, "GuardianId field must be a valid Guid7 ID if provided!"),
        ("name", KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters if provided!")
    };

    private static TeamQueryDto WithInvalidFilters(IEnumerable<string> fields)
    {
        var set = fields.ToHashSet(StringComparer.Ordinal);
        return Query(
            edition: set.Contains("edition") ? Guid.Empty : null,
            school: set.Contains("school") ? Guid.Empty : null,
            captain: set.Contains("captain") ? Guid.Empty : null,
            guardian: set.Contains("guardian") ? Guid.Empty : null,
            name: set.Contains("name") ? new string('T', 51) : null);
    }

    public static IEnumerable<object[]> PairwiseInvalidFilterCases
    {
        get
        {
            for (var i = 0; i < InvalidFilters.Length; i++)
            for (var j = i + 1; j < InvalidFilters.Length; j++)
            {
                var first = InvalidFilters[i];
                var second = InvalidFilters[j];
                yield return new object[]
                {
                    WithInvalidFilters(new[] { first.Field, second.Field }),
                    new[] { (first.Code, first.Message), (second.Code, second.Message) }
                };
            }
        }
    }

    [Fact]
    public void AllOptionalFiltersMayBeAbsent() => ValidationAssertions.IsValid(_validator, Query());

    [Fact]
    public void AllOptionalFiltersMayBeSetToNonEmptyValues() =>
        ValidationAssertions.IsValid(_validator, Query(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('T', 50)));

    [Theory]
    [InlineData("edition", KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID if provided!")]
    [InlineData("school", KoalaValidationErrorCodes.TeamSchoolIdInvalid, "SchoolId field must be a valid Guid7 ID if provided!")]
    [InlineData("captain", KoalaValidationErrorCodes.TeamCaptainIdInvalid, "CaptainId field must be a valid Guid7 ID if provided!")]
    [InlineData("guardian", KoalaValidationErrorCodes.TeamGuardianIdInvalid, "GuardianId field must be a valid Guid7 ID if provided!")]
    public void EmptyOptionalGuid_ReturnsItsInvalidCode(string field, string code, string message)
    {
        var model = field switch
        {
            "edition" => Query(edition: Guid.Empty),
            "school" => Query(school: Guid.Empty),
            "captain" => Query(captain: Guid.Empty),
            "guardian" => Query(guardian: Guid.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        ValidationAssertions.HasError(_validator, model, code, message);
    }

    [Fact]
    public void NameLength51_ReturnsTeamNameTooLong() =>
        ValidationAssertions.HasError(_validator, Query(name: new string('T', 51)), KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters if provided!");

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Team")]
    [InlineData("--------------------------------------------------")]
    public void EmptyOrAllowedNameText_IsAccepted(string name) => ValidationAssertions.IsValid(_validator, Query(name: name));

    [Theory]
    [MemberData(nameof(PairwiseInvalidFilterCases))]
    public void AllErrorsFromTwoInvalidFiltersAreReturned(TeamQueryDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
