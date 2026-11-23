using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class SupplementaryResetPasswordValidationTests
{
    private readonly LinkConsumeResetPasswordRequestDtoValidator _validator = new();

    private static readonly string[] SpecialCharacters =
    [
        "~", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "+", "=", "[", "]",
        "{", "}", ";", ":", "'", "\"", ",", ".", "/"
    ];

    public static IEnumerable<object[]> PasswordCases
    {
        get
        {
            var cases = new List<object[]>();

            // 25 distinct valid values cover the minimum through the middle of the accepted length range.
            for (var length = 8; length <= 32; length++)
                cases.Add(["Aa1!" + new string('b', length - 4), null!]);

            // A second set varies the special character while keeping every other requirement valid.
            foreach (var symbol in SpecialCharacters)
            {
                var length = 8 + cases.Count % 25;
                cases.Add(["Aa1" + symbol + new string('b', length - 4), null!]);
            }

            // Each failure isolates one rule and exercises lengths across the accepted range.
            for (var length = 8; length <= 19; length++)
            {
                cases.Add(["a" + new string('b', length - 3) + "1!", KoalaValidationErrorCodes.PasswordLacksCapitalCharacter]);
                cases.Add(["A" + new string('B', length - 3) + "1!", KoalaValidationErrorCodes.PasswordLacksSmallCharacter]);
                cases.Add(["Aa" + new string('b', length - 4) + "!!", KoalaValidationErrorCodes.PasswordLacksNumberCharacter]);
                cases.Add(["Aa" + new string('b', length - 3) + "1", KoalaValidationErrorCodes.PasswordLacksSpecialCharacter]);
            }

            cases.Add(["Aa1!abc", KoalaValidationErrorCodes.PasswordTooShort]);
            cases.Add(["Aa1!" + new string('b', 47), KoalaValidationErrorCodes.PasswordTooLong]);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(PasswordCases))]
    public void PasswordValidationCoversFiftyValidAndFiftySingleRuleFailures(string password, string? expectedErrorCode)
    {
        if (expectedErrorCode is null)
        {
            ValidationAssertions.IsValid(_validator, new LinkConsumeResetPasswordRequestDto(password));
            return;
        }

        var message = expectedErrorCode switch
        {
            KoalaValidationErrorCodes.PasswordTooShort => "Password field must be at least 8 characters long!",
            KoalaValidationErrorCodes.PasswordTooLong => "Password field cannot be larger than 50 characters!",
            KoalaValidationErrorCodes.PasswordLacksCapitalCharacter => "Password field must contain at least one uppercase letter!",
            KoalaValidationErrorCodes.PasswordLacksSmallCharacter => "Password field must contain at least one lowercase letter!",
            KoalaValidationErrorCodes.PasswordLacksNumberCharacter => "Password field must contain at least one number!",
            _ => "Password field must contain at least one special character!"
        };
        ValidationAssertions.HasError(_validator, new LinkConsumeResetPasswordRequestDto(password), expectedErrorCode, message);
    }
}
