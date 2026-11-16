using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class TeamMemberQueryDtoValidationTests
{
    private readonly TeamMemberQueryDtoValidator _validator = new();

    [Fact]
    public void MissingMemberId_IsAccepted() => ValidationAssertions.IsValid(_validator, new((Guid?)null));

    [Fact]
    public void NonEmptyMemberId_IsAccepted() => ValidationAssertions.IsValid(_validator, new((Guid?)Guid.NewGuid()));

    [Fact]
    public void EmptyMemberId_ReturnsInvalidIdCodeAndMessage() =>
        ValidationAssertions.HasError(_validator, new((Guid?)Guid.Empty), KoalaValidationErrorCodes.TeamMemberIdInvalid, "MemberId field must be a valid Guid7 ID if provided!");
}
