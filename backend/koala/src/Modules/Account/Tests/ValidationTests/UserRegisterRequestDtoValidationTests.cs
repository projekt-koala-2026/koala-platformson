using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserRegisterRequestDtoValidationTests
{
    private readonly UserRegisterRequestDtoValidator _validator = new();
    public static IEnumerable<object[]> SimultaneousEmailAndRolesErrors
    {
        get
        {
            var emails = new[]
            {
                ("", new[] { (KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!") }),
                ("not-an-email", new[] { (KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!") }),
                (new string('a', 321), new[]
                {
                    (KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!"),
                    (KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")
                })
            };
            var roles = new[]
            {
                ((List<string>?)null, new[] { (KoalaValidationErrorCodes.UserRolesEmpty, "Roles field cannot be empty!") }),
                (new List<string>(), new[] { (KoalaValidationErrorCodes.UserRolesEmpty, "Roles field cannot be empty!") }),
                (new List<string> { "" }, new[]
                {
                    (KoalaValidationErrorCodes.UserRoleEmpty, "Role item cannot be empty!"),
                    (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")
                }),
                (new List<string> { "UNKNOWN" }, new[] { (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!") }),
                (new List<string> { new string('R', 51) }, new[]
                {
                    (KoalaValidationErrorCodes.UserRoleTooLong, "Role item name cannot exceed 50 characters!"),
                    (KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")
                })
            };
            foreach (var email in emails)
            foreach (var roleSet in roles)
                yield return new object[]
                {
                    new UserRegisterRequestDto(email.Item1, roleSet.Item1!),
                    email.Item2.Concat(roleSet.Item2).ToArray()
                };
        }
    }

    [Fact]
    public void AllFiveAllowedRolesAndValidEmail_AreAccepted() =>
        ValidationAssertions.IsValid(_validator, new("person@example.com", new()
        {
            RoleNames.NAME_ORGANIZATION_ADMIN,
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_REVIEWER,
            RoleNames.NAME_TEAM_CAPTAIN,
            RoleNames.NAME_TEAM_GUARDIAN
        }));

    [Fact]
    public void EmailAt320Characters_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new(new string('a', 64) + "@" + string.Join('.', Enumerable.Repeat(new string('d', 63), 4)), new() { RoleNames.NAME_TEAM_CAPTAIN }));

    [Theory]
    [InlineData("ORGANIZATION_ADMIN")]
    [InlineData("ORGANIZATION_EDITOR")]
    [InlineData("ORGANIZATION_REVIEWER")]
    [InlineData("TEAM_CAPTAIN")]
    [InlineData("TEAM_GUARDIAN")]
    public void EachAllowedRoleCanBeRegisteredIndividually(string role) =>
        ValidationAssertions.IsValid(_validator, new("person@example.com", new() { role }));

    [Fact]
    public void DuplicateAllowedRolesAreAcceptedByTheDtoValidator() =>
        ValidationAssertions.IsValid(_validator, new("person@example.com", new()
        {
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_EDITOR
        }));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData(" ", KoalaValidationErrorCodes.EmailEmpty, "Email field cannot be empty!")]
    [InlineData("not-an-email", KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!")]
    [InlineData("long-invalid", KoalaValidationErrorCodes.EmailTooLong, "Email field cannot be larger than 320 characters!")]
    public void InvalidEmail_ReturnsExpectedError(string sample, string code, string message)
    {
        var email = sample == "long-invalid" ? new string('a', 321) : sample;
        ValidationAssertions.HasError(_validator, new(email, new() { RoleNames.NAME_TEAM_CAPTAIN }), code, message);
    }

    [Fact]
    public void MissingRoles_ReturnsRolesEmptyError() =>
        ValidationAssertions.HasError(_validator, new("person@example.com", null!), KoalaValidationErrorCodes.UserRolesEmpty, "Roles field cannot be empty!");

    [Fact]
    public void EmptyRoles_ReturnsRolesEmptyError() =>
        ValidationAssertions.HasError(_validator, new("person@example.com", new()), KoalaValidationErrorCodes.UserRolesEmpty, "Roles field cannot be empty!");

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.UserRoleEmpty, "Role item cannot be empty!")]
    [InlineData("INVALID_ROLE", KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")]
    public void EmptyOrUnknownRole_ReturnsExpectedError(string role, string code, string message) =>
        ValidationAssertions.HasError(_validator, new("person@example.com", new() { role }), code, message);

    [Fact]
    public void RoleLength51_ReturnsRoleTooLongError() =>
        ValidationAssertions.HasError(_validator, new("person@example.com", new() { new string('R', 51) }), KoalaValidationErrorCodes.UserRoleTooLong, "Role item name cannot exceed 50 characters!");

    [Theory]
    [MemberData(nameof(SimultaneousEmailAndRolesErrors))]
    public void AllEmailAndRoleErrorsAreReturned(UserRegisterRequestDto dto, (string Code, string Message)[] expected)
    {
        var allExpected = expected.ToList();
        if (dto.Email.Length == 0)
            allExpected.Add((KoalaValidationErrorCodes.EmailInvalid, "Email field must be a valid email address!"));
        ValidationAssertions.HasExactlyErrors(_validator, dto, allExpected.ToArray());
    }
}
