using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserDeclarationUpdateDtoValidationTests
{
    private readonly UserDeclarationUpdateDtoValidator _validator = new();
    public static IEnumerable<object[]> InvalidAgeAndVersionCases
    {
        get
        {
            foreach (var age in new[] { (false, false), (true, true) })
            foreach (var version in new[] { 0, -1, int.MinValue })
                yield return new object[]
                {
                    new UserDeclarationUpdateDto(age.Item1, age.Item2, version),
                    new[]
                    {
                        (KoalaValidationErrorCodes.AgeDeclarationInvalid, "You must confirm your age by checking only one age requirement box!"),
                        (KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")
                    }
                };
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ExactlyOneAgeDeclarationWithPositiveVersion_IsValid(bool age13, bool age18) =>
        ValidationAssertions.IsValid(_validator, new(age13, age18, 1));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void BothOrNeitherAgeDeclarations_ReturnAgeDeclarationError(bool age13, bool age18) =>
        ValidationAssertions.HasError(_validator, new(age13, age18, 1), KoalaValidationErrorCodes.AgeDeclarationInvalid, "You must confirm your age by checking only one age requirement box!");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new(true, false, version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Fact]
    public void MaximumVersion_IsAccepted() => ValidationAssertions.IsValid(_validator, new(false, true, int.MaxValue));

    [Theory]
    [MemberData(nameof(InvalidAgeAndVersionCases))]
    public void AgeAndVersionFailuresAreBothReturned(UserDeclarationUpdateDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
