using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class LinkGenerateResetPasswordRequestDtoValidationTests
{
    private readonly LinkGenerateResetPasswordRequestDtoValidator _validator = new();

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user+tag@example.co.uk")]
    public void ValidEmail_IsAccepted(string email) => ValidationAssertions.IsValid(_validator, new(email));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData("not-an-email", KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")]
    [InlineData("@example.com", KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")]
    [InlineData("user@", KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")]
    public void EmptyOrMalformedEmail_ReturnsExpectedError(string email, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(email), code, message);

    [Fact]
    public void EmailAt320Characters_IsAccepted()
    {
        var domain = string.Join('.', Enumerable.Repeat(new string('d', 63), 3).Append(new string('d', 63)));
        var email = new string('a', 64) + "@" + domain;
        Assert.Equal(320, email.Length);
        ValidationAssertions.IsValid(_validator, new(email));
    }

    [Fact]
    public void EmailAt321Characters_ReturnsTooLongError() =>
        ValidationAssertions.HasError(_validator, new(new string('a', 309) + "@example.com"), KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!");

    [Fact]
    public void OverlongMalformedEmailReturnsLengthAndFormatErrorsTogether() =>
        ValidationAssertions.HasExactlyErrors(_validator, new(new string('a', 321)),
            (KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!"),
            (KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!"));
}
