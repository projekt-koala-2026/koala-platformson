using koala.src.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Core
{
    // 200 -> 399
    public class CoreErrorCodes : KoalaErrorCodes
    {
        public const int EditionNotFound = 200;
        public const int ActiveEditionNotFound = 201;
        public const int ActiveEditionAlreadyExists = 210;
    }
    public class CoreException : Exception
    {
        public int ErrorCode { get; }
        public CoreException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class CoreExceptionHandler : IExceptionHandler
    {
        public CoreExceptionHandler() {}

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int statusCode;
            ApiError apiError;

            if (exception is CoreException coreEx)
            {
                //MAPING FROM EXCEPTION ERROR CODES => STATUS CODES
                statusCode = coreEx.ErrorCode switch
                {
                    CoreErrorCodes.EditionNotFound => StatusCodes.Status404NotFound,
                    CoreErrorCodes.ActiveEditionNotFound => StatusCodes.Status404NotFound,
                    CoreErrorCodes.ActiveEditionAlreadyExists => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                apiError = new ApiError(statusCode, coreEx.Message);
            }
            else
            {
                statusCode = StatusCodes.Status500InternalServerError;
                apiError = new ApiError(statusCode, "Internal server error");
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var response = new ApiResponseWraper<object>(
                Success: false,
                TimeStamp: DateTime.UtcNow,
                Error: apiError,
                Pagination: null,
                Data: null
            );

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }
    }
}