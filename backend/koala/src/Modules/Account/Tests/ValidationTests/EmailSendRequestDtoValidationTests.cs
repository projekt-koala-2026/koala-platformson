using koala.src.Modules.Account.Dtos;
using koala.src.Shared;
using System.Collections.Generic;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

public class EmailSendRequestDtoValidationTests
{
    private readonly EmailSendRequestDtoValidator _validator = new();

    public static IEnumerable<object[]> SimultaneousEmptyFieldCases
    {
        get
        {
            foreach (var subject in new[] { "", " ", "Subject" })
            foreach (var body in new[] { "", "\t", "Body" })
            {
                var expected = new List<(string, string)>();
                if (string.IsNullOrWhiteSpace(subject)) expected.Add((KoalaValidationErrorCodes.EmailSubjectEmpty, "MessageSubject field cannot be empty!"));
                if (string.IsNullOrWhiteSpace(body)) expected.Add((KoalaValidationErrorCodes.EmailBodyEmpty, "MessageBody field cannot be empty!"));
                yield return new object[] { new EmailSendRequestDto(subject, body), expected.ToArray() };
            }
        }
    }

    [Fact]
    public void BothMessagesPresent_AreValid() => ValidationAssertions.IsValid(_validator, new("Subject", "Body"));

    [Theory]
    [InlineData("Weekly update", "Plain text message")]
    [InlineData("Subject with punctuation!?", "<p>HTML body</p>")]
    [InlineData("Żółć — aktualizacja", "Treść wiadomości: ąęćłńóśźż")]
    [InlineData("A", "B")]
    [InlineData("Long subject", "Body with\nmultiple lines\tand whitespace")]
    public void NonEmptySubjectAndBodyVariants_AreAccepted(string subject, string body) =>
        ValidationAssertions.IsValid(_validator, new(subject, body));

    [Theory]
    [InlineData("", "Body", KoalaValidationErrorCodes.EmailSubjectEmpty, "MessageSubject field cannot be empty!")]
    [InlineData("Subject", "", KoalaValidationErrorCodes.EmailBodyEmpty, "MessageBody field cannot be empty!")]
    public void EmptyMessageField_ReturnsItsCodeAndMessage(string subject, string body, string code, string message) =>
        ValidationAssertions.HasError(_validator, new(subject, body), code, message);

    [Fact]
    public void WhitespaceOnlySubject_IsRejectedAsEmpty() =>
        ValidationAssertions.HasError(_validator, new(" ", "Body"), KoalaValidationErrorCodes.EmailSubjectEmpty, "MessageSubject field cannot be empty!");

    [Fact]
    public void WhitespaceOnlyBody_IsRejectedAsEmpty() =>
        ValidationAssertions.HasError(_validator, new("Subject", "\t"), KoalaValidationErrorCodes.EmailBodyEmpty, "MessageBody field cannot be empty!");

    [Theory]
    [MemberData(nameof(SimultaneousEmptyFieldCases))]
    public void SubjectAndBodyErrorsAreReturnedTogether(EmailSendRequestDto dto, (string Code, string Message)[] expected) =>
        ValidationAssertions.HasExactlyErrors(_validator, dto, expected);
}
