using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class LoginRequestDtoValidationTests
{
    private readonly LoginRequestDtoValidator _validator = new();
    public static IEnumerable<object[]> InvalidEmailAndPasswordCases
    {
        get
        {
            var emails = new[]
            {
                ("", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!"),
                (new string('e', 321), KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!")
            };
            var passwords = new[]
            {
                ("", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!"),
                (new string('p', 51), KoalaValidationErrorCodes.PasswordTooLong, "Password field cannot be larger than 50 characters!")
            };
            foreach (var email in emails)
            foreach (var password in passwords)
                yield return new object[]
                {
                    new LoginRequestDto(email.Item1, password.Item1),
                    new[] { (email.Item2, email.Item3), (password.Item2, password.Item3) }
                };
        }
    }

    [Fact]
    public void MaximumLengths_AreAcceptedAndEmailFormatIsNotCheckedByThisValidator() =>
        ValidationAssertions.IsValid(_validator, new("not-an-email", new string('p', 50)));

    [Theory]
    [InlineData("", "password", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData(" ", "password", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData("a@b.test", "", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!")]
    [InlineData("a@b.test", " ", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!")]
    [InlineData("a@b.test", "p", KoalaValidationErrorCodes.PasswordTooLong, "Password field cannot be larger than 50 characters!")]
    public void EmptyFieldReturnsExpectedError(string email, string password, string code, string message)
    {
        if (code == KoalaValidationErrorCodes.PasswordTooLong)
            password = new string('p', 51);
        ValidationAssertions.HasError(_validator, new(email, password), code, message);
    }

    [Fact]
    public void EmailOver320Characters_ReturnsEmailTooLongError() =>
        ValidationAssertions.HasError(_validator, new(new string('a', 321), "password"), KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!");

    [Fact]
    public void EmailAt320Characters_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new(new string('a', 320), "password"));

    [Theory]
    [MemberData(nameof(InvalidEmailAndPasswordCases))]
    public void EmailAndPasswordErrorsAreBothReturned(LoginRequestDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
