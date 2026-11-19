using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class RodoStateUpdateDtoValidationTests
{
    private readonly RodoStateUpdateDtoValidator _validator = new();
    public static IEnumerable<object[]> InvalidStateAndVersionCases
    {
        get
        {
            var states = new[]
            {
                ("", new[]
                {
                    (KoalaValidationErrorCodes.RodoStateEmpty, "State field cannot be empty!"),
                    (KoalaValidationErrorCodes.RodoStateInvalid, "State field must be either RODO_ACCEPTED or RODO_REJECTED!")
                }),
                ("PENDING", new[]
                {
                    (KoalaValidationErrorCodes.RodoStateInvalid, "State field must be either RODO_ACCEPTED or RODO_REJECTED!")
                }),
                (new string('A', 21), new[]
                {
                    (KoalaValidationErrorCodes.RodoStateTooLong, "State field cannot exceed 20 characters!"),
                    (KoalaValidationErrorCodes.RodoStateInvalid, "State field must be either RODO_ACCEPTED or RODO_REJECTED!")
                })
            };
            foreach (var state in states)
            foreach (var version in new[] { 0, -1, int.MinValue })
                yield return new object[]
                {
                    new RodoStateUpdateDto(state.Item1, RodoVersion: 1, Version: version),
                    state.Item2.Append((KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0")).ToArray()
                };
        }
    }

    [Theory]
    [InlineData(TeamMemberRodoState.RODOSTATE_ACCEPETED)]
    [InlineData(TeamMemberRodoState.RODOSTATE_REJECTED)]
    public void SupportedStatesAndPositiveVersions_AreValid(string state) =>
        ValidationAssertions.IsValid(_validator, new(state, RodoVersion: 1, Version: 1));

    [Theory]
    [InlineData("", KoalaValidationErrorCodes.RodoStateEmpty, "State field cannot be empty!")]
    [InlineData("PENDING", KoalaValidationErrorCodes.RodoStateInvalid, "State field must be either RODO_ACCEPTED or RODO_REJECTED!")]
    public void EmptyOrUnknownState_ReturnsExpectedError(string state, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(state, RodoVersion: 1, Version: 1), code, message);

    [Fact]
    public void StateLength20_IsWithinLengthLimitButStillMustBeAnAllowedState()
    {
        var result = _validator.Validate(new RodoStateUpdateDto(new string('A', 20), RodoVersion: 1, Version: 1));
        Assert.DoesNotContain(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.RodoStateTooLong);
        Assert.Contains(result.Errors, error => error.ErrorCode == KoalaValidationErrorCodes.RodoStateInvalid && error.ErrorMessage == "State field must be either RODO_ACCEPTED or RODO_REJECTED!");
    }

    [Fact]
    public void StateLength21_ReturnsTooLongError() =>
        ValidationAssertions.HasError(_validator, new(new string('A', 21), RodoVersion: 1, Version: 1), KoalaValidationErrorCodes.RodoStateTooLong, "State field cannot exceed 20 characters!");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveVersion_ReturnsResourceVersionError(int version) =>
        ValidationAssertions.HasError(_validator, new(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 1, Version: version), KoalaValidationErrorCodes.ResourceVersionInvalid, "Version field must be greater than 0");

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void PositiveVersionBoundary_IsAccepted(int version) =>
        ValidationAssertions.IsValid(_validator, new(TeamMemberRodoState.RODOSTATE_REJECTED, RodoVersion: 1, Version: version));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveRodoVersion_ReturnsDocumentVersionError(int rodoVersion)
    {
        var result = _validator.Validate(new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: rodoVersion, Version: 1));
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(RodoStateUpdateDto.RodoVersion), error.PropertyName);
        Assert.Equal(KoalaValidationErrorCodes.ResourceVersionInvalid, error.ErrorCode);
        Assert.Equal("RodoVersion field must be greater than 0", error.ErrorMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void PositiveRodoVersionBoundary_IsAccepted(int rodoVersion) =>
        ValidationAssertions.IsValid(_validator, new(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: rodoVersion, Version: 1));

    [Fact]
    public void BothVersionsInvalid_ReturnsBothFieldErrors()
    {
        var result = _validator.Validate(new RodoStateUpdateDto(TeamMemberRodoState.RODOSTATE_ACCEPETED, RodoVersion: 0, Version: 0));
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RodoStateUpdateDto.RodoVersion));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RodoStateUpdateDto.Version));
    }

    [Theory]
    [MemberData(nameof(InvalidStateAndVersionCases))]
    public void AllStateAndVersionErrorsAreReturned(RodoStateUpdateDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
