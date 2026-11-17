using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace koala.src.Modules.Account.Tests.ValidationTests;

internal static class ValidationAssertions
{
    public static void IsValid<T>(IValidator<T> validator, T model)
    {
        var result = validator.Validate(model);
        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(error => error.ErrorMessage)));
    }

    public static void HasError<T>(IValidator<T> validator, T model, string code, string message)
    {
        ValidationResult result = validator.Validate(model);
        Assert.Contains(result.Errors, error => error.ErrorCode == code && error.ErrorMessage == message);
    }

    public static void HasMessage<T>(IValidator<T> validator, T model, string message)
    {
        ValidationResult result = validator.Validate(model);
        Assert.Contains(result.Errors, error => error.ErrorMessage == message);
    }

    public static void HasExactlyErrors<T>(IValidator<T> validator, T model, params (string Code, string Message)[] expected)
    {
        ValidationResult result = validator.Validate(model);
        var actual = result.Errors
            .Select(error => (error.ErrorCode, error.ErrorMessage))
            .OrderBy(error => error.ErrorCode, StringComparer.Ordinal)
            .ThenBy(error => error.ErrorMessage, StringComparer.Ordinal)
            .ToArray();
        var orderedExpected = expected
            .OrderBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)
            .Select(error => (error.Code, error.Message))
            .ToArray();
        Assert.Equal(orderedExpected, actual);
    }

    public static void HasExactlyMessages<T>(IValidator<T> validator, T model, params string[] expected)
    {
        ValidationResult result = validator.Validate(model);
        var actual = result.Errors.Select(error => error.ErrorMessage)
            .OrderBy(message => message, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.OrderBy(message => message, StringComparer.Ordinal), actual);
    }
}
