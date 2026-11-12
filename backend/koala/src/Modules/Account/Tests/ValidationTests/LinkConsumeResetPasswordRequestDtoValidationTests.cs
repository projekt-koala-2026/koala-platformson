using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class LinkConsumeResetPasswordRequestDtoValidationTests
{
    private readonly LinkConsumeResetPasswordRequestDtoValidator _validator = new();

    [Fact]
    public void PasswordAtMinimumLength_IsAccepted() => ValidationAssertions.IsValid(_validator, new("Abcdef1!"));

    [Fact]
    public void PasswordAtMaximumLength_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new("Aa1!" + new string('b', 46)));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!")]
    [InlineData("Ab1!xyz", KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!")]
    public void EmptyOrShortPassword_ReturnsExpectedError(string password, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(password), code, message);

    [Fact]
    public void PasswordOver50Characters_ReturnsTooLongError() =>
        ValidationAssertions.HasError(_validator, new("Aa1!" + new string('b', 47)), KoalaValidationErrorCodes.PasswordTooLong, "Password field cannot be larger than 50 characters!");

    [Theory]
    [InlineData("abcdefg1!", KoalaValidationErrorCodes.PasswordLacksCapitalCharacter, "Password field must contain at least one uppercase letter!")]
    [InlineData("ABCDEFG1!", KoalaValidationErrorCodes.PasswordLacksSmallCharacter, "Password field must contain at least one lowercase letter!")]
    [InlineData("Abcdefgh!", KoalaValidationErrorCodes.PasswordLacksNumberCharacter, "Password field must contain at least one number!")]
    [InlineData("Abcdefgh1", KoalaValidationErrorCodes.PasswordLacksSpecialCharacter, "Password field must contain at least one special character!")]
    public void MissingCharacterClass_ReturnsExpectedError(string password, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(password), code, message);

    [Fact]
    public void VeryShortPasswordReturnsEveryLengthAndCharacterClassError() =>
        ValidationAssertions.HasExactlyErrors(_validator, new("abc"),
            (KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!"),
            (KoalaValidationErrorCodes.PasswordLacksCapitalCharacter, "Password field must contain at least one uppercase letter!"),
            (KoalaValidationErrorCodes.PasswordLacksNumberCharacter, "Password field must contain at least one number!"),
            (KoalaValidationErrorCodes.PasswordLacksSpecialCharacter, "Password field must contain at least one special character!"));
}
