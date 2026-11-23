using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class AdminTeamQueryValidationMatrixTests
{
    private readonly AdminTeamQueryDtoValidator _validator = new();

    public static IEnumerable<object[]> QueryCases
    {
        get
        {
            for (var i = 0; i < 25; i++)
            {
                Guid? edition = i % 3 == 0 ? null : Guid.CreateVersion7();
                Guid? school = i % 4 == 0 ? null : Guid.CreateVersion7();
                string? name = i % 5 == 0 ? null : $"Valid team name {i:D2}";
                yield return [new AdminTeamQueryDto(edition, school, name), Array.Empty<string>()];
            }

            for (var i = 0; i < 20; i++)
                yield return [new AdminTeamQueryDto(Guid.Empty, i % 2 == 0 ? null : Guid.CreateVersion7(), $"Edition error {i:D2}"), new[] { KoalaValidationErrorCodes.TeamEditionIdInvalid }];

            for (var i = 0; i < 20; i++)
                yield return [new AdminTeamQueryDto(i % 2 == 0 ? null : Guid.CreateVersion7(), Guid.Empty, $"School error {i:D2}"), new[] { KoalaValidationErrorCodes.TeamSchoolIdInvalid }];

            for (var length = 51; length <= 70; length++)
                yield return [new AdminTeamQueryDto(null, null, new string('N', length)), new[] { KoalaValidationErrorCodes.TeamNameTooLong }];

            for (var i = 0; i < 5; i++)
                yield return [new AdminTeamQueryDto(Guid.Empty, Guid.Empty, $"Combined ids {i}"), new[] { KoalaValidationErrorCodes.TeamEditionIdInvalid, KoalaValidationErrorCodes.TeamSchoolIdInvalid }];

            for (var length = 51; length <= 55; length++)
                yield return [new AdminTeamQueryDto(Guid.Empty, Guid.CreateVersion7(), new string('E', length)), new[] { KoalaValidationErrorCodes.TeamEditionIdInvalid, KoalaValidationErrorCodes.TeamNameTooLong }];

            for (var length = 56; length <= 60; length++)
                yield return [new AdminTeamQueryDto(Guid.CreateVersion7(), Guid.Empty, new string('S', length)), new[] { KoalaValidationErrorCodes.TeamSchoolIdInvalid, KoalaValidationErrorCodes.TeamNameTooLong }];
        }
    }

    [Theory]
    [MemberData(nameof(QueryCases))]
    public void AdminTeamOptionalFiltersReportAllExpectedErrorCombinations(AdminTeamQueryDto query, string[] expectedCodes)
    {
        var result = _validator.Validate(query);
        var actualCodes = result.Errors.Select(error => error.ErrorCode).OrderBy(code => code, StringComparer.Ordinal).ToArray();
        var sortedExpectedCodes = expectedCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.Equal(sortedExpectedCodes, actualCodes);
        Assert.Equal(expectedCodes.Length == 0, result.IsValid);
    }
}
