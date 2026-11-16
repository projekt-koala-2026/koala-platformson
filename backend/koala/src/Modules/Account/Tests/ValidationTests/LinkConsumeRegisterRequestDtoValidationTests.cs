using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class LinkConsumeRegisterRequestDtoValidationTests
{
    private readonly LinkConsumeRegisterRequestDtoValidator _validator = new();
    private static LinkConsumeRegisterRequestDto Valid(string first = "Jan", string last = "Nowak", string password = "Strong1!", bool rodo = true, bool rules = true, bool age13 = true, bool age18 = false) =>
        new(first, last, password, rodo, rules, age13, age18);

    private static readonly (string Value, string Code, string Message)[] FirstNameFailures =
    {
        ("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!"),
        (new string('A', 51), KoalaValidationErrorCodes.NameFirstTooLong, "NameFirst field cannot be larger than 50 characters!"),
        ("Jan1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (string Value, string Code, string Message)[] LastNameFailures =
    {
        ("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!"),
        (new string('B', 51), KoalaValidationErrorCodes.NameLastTooLong, "NameLast field cannot be larger than 50 characters!"),
        ("Nowak_", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")
    };
    private static readonly (string Value, string Code, string Message)[] PasswordFailures =
    {
        ("", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!"),
        ("Ab1!xy", KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!"),
        ("Aa1!" + new string('b', 47), KoalaValidationErrorCodes.PasswordTooLong, "Password field cannot be larger than 50 characters!"),
        ("abcdefg1!", KoalaValidationErrorCodes.PasswordLacksCapitalCharacter, "Password field must contain at least one uppercase letter!"),
        ("ABCDEFG1!", KoalaValidationErrorCodes.PasswordLacksSmallCharacter, "Password field must contain at least one lowercase letter!"),
        ("Abcdefgh!", KoalaValidationErrorCodes.PasswordLacksNumberCharacter, "Password field must contain at least one number!"),
        ("Abcdefgh1", KoalaValidationErrorCodes.PasswordLacksSpecialCharacter, "Password field must contain at least one special character!")
    };

    public static IEnumerable<object[]> NameAndPasswordErrorCases
    {
        get
        {
            foreach (var first in FirstNameFailures)
            foreach (var last in LastNameFailures)
                yield return new object[] { Valid(first: first.Value, last: last.Value), new[] { (first.Code, first.Message), (last.Code, last.Message) } };

            foreach (var first in FirstNameFailures)
            foreach (var password in PasswordFailures)
                yield return new object[] { Valid(first: first.Value, password: password.Value), new[] { (first.Code, first.Message), (password.Code, password.Message) } };

            foreach (var last in LastNameFailures)
            foreach (var password in PasswordFailures)
                yield return new object[] { Valid(last: last.Value, password: password.Value), new[] { (last.Code, last.Message), (password.Code, password.Message) } };
        }
    }

    public static IEnumerable<object[]> ConsentAndAgeErrorCases
    {
        get
        {
            foreach (var rodo in new[] { false, true })
            foreach (var rules in new[] { false, true })
            foreach (var age in new[] { (false, false), (true, true) })
            {
                var expected = new List<(string, string)>();
                if (!rodo) expected.Add((KoalaValidationErrorCodes.RodoNotAccepted, "AcceptedRodo field must be true!"));
                if (!rules) expected.Add((KoalaValidationErrorCodes.RulesNotAccepted, "AcceptedRules field must be true!"));
                expected.Add((KoalaValidationErrorCodes.AgeDeclarationInvalid, "You must confirm your age by checking only one age requirement box!"));
                yield return new object[] { Valid(rodo: rodo, rules: rules, age13: age.Item1, age18: age.Item2), expected.ToArray() };
            }
        }
    }

    [Theory]
    [InlineData("A", "B", "Abcdef1!", true, false)]
    [InlineData("A", "B", "Abcdef1!", false, true)]
    [InlineData("A", "B", "Abcdef1!", true, false)]
    [InlineData("A", "B", "AbcdefghijklmnopqrstuvwxyzABCDEFGH1!", true, false)]
    public void ValidBoundariesAndExactlyOneAgeDeclaration_AreAccepted(string first, string last, string password, bool age13, bool age18) =>
        ValidationAssertions.IsValid(_validator, Valid(first, last, password, age13: age13, age18: age18));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameFirstEmpty, "NameFirst field cannot be empty!")]
    [InlineData("Jan1", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Jan@", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("Jan_", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    public void InvalidFirstName_ReturnsNameError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, Valid(first: name), code, message);

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.NameLastEmpty, "NameLast field cannot be empty!")]
    [InlineData("Nowak1", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    [InlineData("Nowak.", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    public void InvalidLastName_ReturnsNameError(string name, string code, string message) =>
        ValidationAssertions.HasError(_validator, Valid(last: name), code, message);

    [Theory]
    [InlineData(49)]
    [InlineData(50)]
    public void NameLengthAtOrBelowMaximum_IsAccepted(int length) =>
        ValidationAssertions.IsValid(_validator, Valid(new string('Ż', length), new string('Ł', length)));

    [Fact]
    public void NamesOverMaximumLength_ReturnTheirTooLongCodes()
    {
        var result = _validator.Validate(Valid(new string('A', 51), new string('B', 51)));
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameFirstTooLong && error.ErrorMessage == "NameFirst field cannot be larger than 50 characters!");
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.NameLastTooLong && error.ErrorMessage == "NameLast field cannot be larger than 50 characters!");
    }

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.PasswordEmpty, "Password field cannot be empty!")]
    [InlineData("Ab1!xyz", KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!")]
    [InlineData("Ab1!" + "x", KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!")]
    public void InvalidPasswordLength_ReturnsPasswordError(string password, string code, string message) =>
        ValidationAssertions.HasError(_validator, Valid(password: password), code, message);

    [Theory]
    [InlineData("abcdefg1!")]
    [InlineData("12345678!")]
    [InlineData("ABCDEFG1!")]
    [InlineData("Abcdefgh1")]
    public void MissingPasswordCharacterClass_ReturnsItsCodeAndMessage(string password)
    {
        var result = _validator.Validate(Valid(password: password));
        var expected = password switch
        {
            "abcdefg1!" => (KoalaValidationErrorCodes.PasswordLacksCapitalCharacter, "Password field must contain at least one uppercase letter!"),
            "12345678!" => (KoalaValidationErrorCodes.PasswordLacksSmallCharacter, "Password field must contain at least one lowercase letter!"),
            "ABCDEFG1!" => (KoalaValidationErrorCodes.PasswordLacksSmallCharacter, "Password field must contain at least one lowercase letter!"),
            "Abcdefgh1" => (KoalaValidationErrorCodes.PasswordLacksSpecialCharacter, "Password field must contain at least one special character!"),
            _ => throw new InvalidOperationException()
        };
        Assert.Contains(result.Errors, error => error.ErrorCode == expected.Item1 && error.ErrorMessage == expected.Item2);
    }

    [Fact]
    public void PasswordMissingNumber_ReturnsNumberCodeAndMessage() =>
        ValidationAssertions.HasError(_validator, Valid(password: "Abcdefgh!"), KoalaValidationErrorCodes.PasswordLacksNumberCharacter, "Password field must contain at least one number!");

    [Fact]
    public void PasswordOverMaximumLength_ReturnsTooLongCodeAndMessage() =>
        ValidationAssertions.HasError(_validator, Valid(password: "Aa1!" + new string('b', 47)), KoalaValidationErrorCodes.PasswordTooLong, "Password field cannot be larger than 50 characters!");

    [Fact]
    public void PasswordAtMaximumLength_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, Valid(password: "Aa1!" + new string('b', 46)));

    [Theory]
    [InlineData(false, true, KoalaValidationErrorCodes.RodoNotAccepted, "AcceptedRodo field must be true!")]
    [InlineData(true, false, KoalaValidationErrorCodes.RulesNotAccepted, "AcceptedRules field must be true!")]
    public void RequiredConsents_ReturnTheirCodeAndMessage(bool rodo, bool rules, string code, string message) =>
        ValidationAssertions.HasError(_validator, Valid(rodo: rodo, rules: rules), code, message);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AgeDeclarationsMustBeExclusive(bool age13, bool age18) =>
        ValidationAssertions.HasError(_validator, Valid(age13: age13, age18: age18), KoalaValidationErrorCodes.AgeDeclarationInvalid, "You must confirm your age by checking only one age requirement box!");

    [Theory]
    [MemberData(nameof(NameAndPasswordErrorCases))]
    public void NameAndPasswordFailuresAreAllReturned(LinkConsumeRegisterRequestDto dto, (string Code, string Message)[] expected)
    {
        var allExpected = expected.ToList();
        if (dto.NameFirst.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!"));
        if (dto.NameLast.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!"));
        if (dto.Password.Length == 0)
        {
            allExpected.Add((KoalaValidationErrorCodes.PasswordTooShort, "Password field must be at least 8 characters long!"));
            allExpected.Add((KoalaValidationErrorCodes.PasswordLacksCapitalCharacter, "Password field must contain at least one uppercase letter!"));
            allExpected.Add((KoalaValidationErrorCodes.PasswordLacksSmallCharacter, "Password field must contain at least one lowercase letter!"));
            allExpected.Add((KoalaValidationErrorCodes.PasswordLacksNumberCharacter, "Password field must contain at least one number!"));
            allExpected.Add((KoalaValidationErrorCodes.PasswordLacksSpecialCharacter, "Password field must contain at least one special character!"));
        }
        ValidationAssertions.HasExactlyErrors(_validator, dto, allExpected.ToArray());
    }

    [Theory]
    [MemberData(nameof(ConsentAndAgeErrorCases))]
    public void ConsentAndAgeFailuresAreAllReturned(LinkConsumeRegisterRequestDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
