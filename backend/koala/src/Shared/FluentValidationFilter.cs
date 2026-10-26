using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace koala.src.Shared.Validation;

public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>)
                .MakeGenericType(argument.GetType());

            if (context.HttpContext.RequestServices
                    .GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);

            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                if (!errors.TryGetValue(error.PropertyName, out var messages))
                {
                    messages = [];
                    errors[error.PropertyName] = messages;
                }

                messages.Add(error.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(
                errors.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed"
            };

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}