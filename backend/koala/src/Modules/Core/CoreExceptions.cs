using koala.src.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Core
{
    // 600 -> 799
    public class CoreErrorCodes : KoalaErrorCodes
    {
        public const int EditionNotFound = 600;
        public const int ActiveEditionNotFound = 601;
        public const int SchoolNotFound = 602;
        public const int ActiveEditionAlreadyExists = 610;
        public const int SchoolAlreadyExists = 611;
        public const int InvalidImportSchoolFile = 612;
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
            return false;
        }
    }
}