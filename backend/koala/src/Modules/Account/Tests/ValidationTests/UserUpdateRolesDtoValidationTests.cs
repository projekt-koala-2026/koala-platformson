using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class UserUpdateRolesDtoValidationTests
{
    private readonly UserUpdateRolesDtoValidator _validator = new();
    public static IEnumerable<object[]> SimultaneousRolesAndVersionErrors
    {
        get
        {
            var roleSets = new[]
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
            var versions = new[] { 0, -1, int.MinValue };
            foreach (var roles in roleSets)
            foreach (var version in versions)
                yield return new object[]
                {
                    new UserUpdateRolesDto(roles.Item1!, version),
                    roles.Item2.Append((KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")).ToArray()
                };
        }
    }

    [Fact]
    public void AllAllowedRolesAndPositiveVersion_AreAccepted() =>
        ValidationAssertions.IsValid(_validator, new(new()
        {
            RoleNames.NAME_ORGANIZATION_ADMIN,
            RoleNames.NAME_ORGANIZATION_EDITOR,
            RoleNames.NAME_ORGANIZATION_REVIEWER,
            RoleNames.NAME_TEAM_CAPTAIN,
            RoleNames.NAME_TEAM_GUARDIAN
        }, 1));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.UserRolesEmpty, "Roles field cannot be empty!")]
    [InlineData("INVALID_ROLE", KoalaValidationErrorCodes.UserRoleInvalid, "Invalid role specified!")]
    public void EmptyCollectionOrUnknownRole_ReturnsExpectedError(string rolesValue, string code, string message)
    {
        var roles = rolesValue.Length == 0 ? new List<string>() : new List<string> { rolesValue };
        ValidationAssertions.HasError(_validator, new(roles, 1), code, message);
    }

    [Fact]
    public void EmptyItem_ReturnsRoleEmptyError() =>
        ValidationAssertions.HasError(_validator, new(new() { "" }, 1), KoalaValidationErrorCodes.UserRoleEmpty, "Role item cannot be empty!");

    [Fact]
    public void RoleLength51_ReturnsRoleTooLongError() =>
        ValidationAssertions.HasError(_validator, new(new() { new string('R', 51) }, 1), KoalaValidationErrorCodes.UserRoleTooLong, "Role item name cannot exceed 50 characters!");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new(new() { RoleNames.NAME_TEAM_CAPTAIN }, version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Fact]
    public void MaximumVersion_IsAccepted() =>
        ValidationAssertions.IsValid(_validator, new(new() { RoleNames.NAME_TEAM_CAPTAIN }, int.MaxValue));

    [Theory]
    [InlineData("ORGANIZATION_ADMIN")]
    [InlineData("ORGANIZATION_EDITOR")]
    [InlineData("ORGANIZATION_REVIEWER")]
    [InlineData("TEAM_CAPTAIN")]
    [InlineData("TEAM_GUARDIAN")]
    public void EachAllowedRoleCanBeUpdatedIndividually(string role) =>
        ValidationAssertions.IsValid(_validator, new(new() { role }, 1));

    [Fact]
    public void DuplicateAllowedRolesAreAcceptedByTheDtoValidator() =>
        ValidationAssertions.IsValid(_validator, new(new()
        {
            RoleNames.NAME_TEAM_GUARDIAN,
            RoleNames.NAME_TEAM_GUARDIAN
        }, 1));

    [Theory]
    [MemberData(nameof(SimultaneousRolesAndVersionErrors))]
    public void AllRoleAndVersionErrorsAreReturned(UserUpdateRolesDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
