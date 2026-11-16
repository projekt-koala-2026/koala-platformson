using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserQueryDtoValidationTests
{
    private readonly UserQueryDtoValidator _validator = new();
    private static UserQueryDto Query(string? email = null, string? first = null, string? last = null, List<string>? roles = null) =>
        new(email, first, last, roles, null);

    public static IEnumerable<object[]> CombinedInvalidFilterCases
    {
        get
        {
            var emails = new[]
            {
                ("not-an-email", new[] { (KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!") }),
                (new string('a', 321), new[]
                {
                    (KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!"),
                    (KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")
                })
            };
            var firstNames = new[]
            {
                ("First1", new[] { (KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!") }),
                (new string('A', 51), new[] { (KoalaValidationErrorCodes.NameFirstTooLong, "NameFirst field cannot be larger than 50 characters!") })
            };
            var lastNames = new[]
            {
                ("Last_", new[] { (KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!") }),
                (new string('B', 51), new[] { (KoalaValidationErrorCodes.NameLastTooLong, "NameLast field cannot be larger than 50 characters!") })
            };
            var roleFilters = new[]
            {
                (new List<string> { "UNKNOWN" }, new[] { (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!") }),
                (new List<string> { "" }, new[]
                {
                    (KoalaValidationErrorCodes.UserRoleEmpty, "Role item cannot be empty!"),
                    (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")
                }),
                (new List<string> { new string('R', 51) }, new[]
                {
                    (KoalaValidationErrorCodes.UserRoleTooLong, "Role item name cannot exceed 50 characters!"),
                    (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")
                })
            };

            foreach (var email in emails)
            foreach (var first in firstNames)
            foreach (var last in lastNames)
            foreach (var roles in roleFilters)
                yield return new object[]
                {
                    Query(email.Item1, first.Item1, last.Item1, roles.Item1),
                    email.Item2.Concat(first.Item2).Concat(last.Item2).Concat(roles.Item2).ToArray()
                };
        }
    }

    [Fact]
    public void OmittedOrEmptyOptionalFilters_AreAccepted() =>
        ValidationAssertions.IsValid(_validator, Query("", "", "", new()));

    [Theory]
    [InlineData("ORGANIZATION_ADMIN")]
    [InlineData("ORGANIZATION_EDITOR")]
    [InlineData("ORGANIZATION_REVIEWER")]
    [InlineData("TEAM_CAPTAIN")]
    [InlineData("TEAM_GUARDIAN")]
    public void EveryAllowedRole_IsAccepted(string role) =>
        ValidationAssertions.IsValid(_validator, Query(roles: new() { role }));

    [Theory]
    [InlineData(" ")]
    [InlineData("Jan-Żółć")]
    public void NamesContainingOnlyAllowedUnicodeLettersSpacesAndHyphens_AreAccepted(string name) =>
        ValidationAssertions.IsValid(_validator, Query(first: name, last: name));

    [Fact]
    public void NamesAtMaximumLength_AreAccepted() =>
        ValidationAssertions.IsValid(_validator, Query(first: new string('A', 50), last: new string('B', 50)));

    [Theory]
    [InlineData("first-too-long", KoalaValidationErrorCodes.NameFirstTooLong, "NameFirst field cannot be larger than 50 characters!")]
    [InlineData("first-invalid", KoalaValidationErrorCodes.NameFirstInvalidCharacter, "NameFirst field can only contain letters, spaces, or hyphens!")]
    [InlineData("last-too-long", KoalaValidationErrorCodes.NameLastTooLong, "NameLast field cannot be larger than 50 characters!")]
    [InlineData("last-invalid", KoalaValidationErrorCodes.NameLastInvalidCharacter, "NameLast field can only contain letters, spaces, or hyphens!")]
    public void InvalidNameFilter_ReturnsExpectedError(string caseName, string code, string message)
    {
        var model = caseName switch
        {
            "first-too-long" => Query(first: new string('A', 51)),
            "first-invalid" => Query(first: "Jan1"),
            "last-too-long" => Query(last: new string('B', 51)),
            _ => Query(last: "Nowak_")
        };
        ValidationAssertions.HasError(_validator, model, code, message);
    }

    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("")]
    public void OptionalEmailWhenValidOrEmpty_IsAccepted(string email) =>
        ValidationAssertions.IsValid(_validator, Query(email: email));

    [Theory]
    [InlineData("not-an-email", KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")]
    [InlineData("long-invalid", KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!")]
    public void InvalidEmailFilter_ReturnsExpectedError(string sample, string code, string message)
    {
        var email = sample == "long-invalid" ? new string('a', 321) : sample;
        ValidationAssertions.HasError(_validator, Query(email: email), code, message);
    }

    [Fact]
    public void EmailAt320Characters_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, Query(email: new string('a', 64) + "@" + string.Join('.', Enumerable.Repeat(new string('d', 63), 4))));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.UserRoleEmpty, "Role item cannot be empty!")]
    [InlineData("SOME_ROLE", KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")]
    public void InvalidRoleFilter_ReturnsExpectedError(string role, string code, string message) =>
        ValidationAssertions.HasError(_validator, Query(roles: new() { role }), code, message);

    [Fact]
    public void RoleFilterLongerThan50_ReturnsTooLongError() =>
        ValidationAssertions.HasError(_validator, Query(roles: new() { new string('R', 51) }), KoalaValidationErrorCodes.UserRoleTooLong, "Role item name cannot exceed 50 characters!");

    [Theory]
    [MemberData(nameof(CombinedInvalidFilterCases))]
    public void EveryInvalidQueryFilterReportsAllItsErrors(UserQueryDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
