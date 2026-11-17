using koala.src.Modules.Cms.Dtos;
using koala.src.Shared;
using Xunit;

namespace koala.src.Modules.Cms.Tests.ValidationTests;

public class PostQueryDtoValidationTests
{
    private static PostQueryDtoDtoValidator Validator => new();
    private static PostQueryDto Model(Guid? edition = null, string? name = null, bool? hidden = null) => new(edition, name, hidden);

    [Fact] public void AcceptsAllOptionalFieldsMissing() => ValidationAssertions.IsValid(Validator, Model());
    [Theory] [InlineData(false)] [InlineData(true)]
    public void AcceptsEitherShowHiddenValue(bool value) => ValidationAssertions.IsValid(Validator, Model(hidden: value));
    [Theory]
    [InlineData("", false)] [InlineData(" ", false)] [InlineData("A", false)] [InlineData("x", false)] [InlineData("Post", false)]
    [InlineData("Żółć", false)] [InlineData("Name With Spaces", false)] [InlineData("Name-Here", false)]
    [InlineData("", true)] [InlineData(" ", true)] [InlineData("A", true)] [InlineData("x", true)] [InlineData("Post", true)]
    [InlineData("Żółć", true)] [InlineData("Name With Spaces", true)] [InlineData("Name-Here", true)]
    public void AcceptsNamesThroughTheLimit(string name, bool setEdition)
    {
        var dto = Model(setEdition ? Guid.NewGuid() : null, name);
        ValidationAssertions.IsValid(Validator, dto);
    }
    [Theory]
    [InlineData(49)] [InlineData(50)]
    public void AcceptsMaximumNameBoundary(int length) => ValidationAssertions.IsValid(Validator, Model(name: new string('a', length)));
    [Theory]
    [InlineData(51)] [InlineData(100)] [InlineData(500)]
    public void RejectsNameAboveMaximumLength(int length) => ValidationAssertions.HasError(Validator, Model(name: new string('a', length)), KoalaValidationErrorCodes.PostNameTooLong, "Name field cannot be larger than 50 characters!");
    [Fact] public void AcceptsNonEmptyEditionId() => ValidationAssertions.IsValid(Validator, Model(edition: Guid.NewGuid()));
    [Fact] public void RejectsEmptyEditionId() => ValidationAssertions.HasError(Validator, Model(edition: Guid.Empty), KoalaValidationErrorCodes.TeamEditionIdInvalid, "EditionId field must be a valid Guid7 ID!");
    [Fact] public void OptionalNullEditionIdDoesNotTriggerValidation() => ValidationAssertions.IsValid(Validator, Model(edition: null));
    [Fact]
    public void ReportsNameAndEditionErrorsTogether()
    {
        var result = Validator.Validate(Model(Guid.Empty, new string('x', 51)));
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.PostNameTooLong && e.ErrorMessage == "Name field cannot be larger than 50 characters!");
        Assert.Contains(result.Errors, e => e.ErrorCode == KoalaValidationErrorCodes.TeamEditionIdInvalid && e.ErrorMessage == "EditionId field must be a valid Guid7 ID!");
    }
}
