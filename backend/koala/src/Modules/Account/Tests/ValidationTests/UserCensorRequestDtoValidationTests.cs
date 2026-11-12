using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserCensorRequestDtoValidationTests
{
    private readonly UserCensorRequestDtoValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void PositiveVersion_IsAccepted(int version) => ValidationAssertions.IsValid(_validator, new(version));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new(version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");
}
