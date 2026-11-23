using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class AdminTeamQueryDtoValidationTests
{
    private readonly AdminTeamQueryDtoValidator _validator = new();

    [Fact]
    public void AllOptionalFiltersMayBeAbsentOrValid() =>
        ValidationAssertions.IsValid(_validator, new AdminTeamQueryDto(null, null, null));

    [Fact]
    public void AcceptsValidOptionalFilters() =>
        ValidationAssertions.IsValid(_validator, new AdminTeamQueryDto(Guid.NewGuid(), Guid.NewGuid(), new string('T', 50)));

    [Theory]
    [InlineData(true, false, KoalaValidationErrorCodes.TeamEditionIdInvalid)]
    [InlineData(false, true, KoalaValidationErrorCodes.TeamSchoolIdInvalid)]
    public void RejectsEmptyOptionalIds(bool invalidEdition, bool invalidSchool, string errorCode) =>
        ValidationAssertions.HasError(_validator,
            new AdminTeamQueryDto(invalidEdition ? Guid.Empty : Guid.NewGuid(), invalidSchool ? Guid.Empty : Guid.NewGuid(), null),
            errorCode,
            invalidEdition ? "EditionId field must be a valid Guid7 ID if provided!" : "SchoolId field must be a valid Guid7 ID if provided!");

    [Fact]
    public void RejectsNameLongerThanFiftyCharacters() =>
        ValidationAssertions.HasError(_validator, new AdminTeamQueryDto(null, null, new string('T', 51)),
            KoalaValidationErrorCodes.TeamNameTooLong, "Name field cannot be larger than 50 characters if provided!");
}
