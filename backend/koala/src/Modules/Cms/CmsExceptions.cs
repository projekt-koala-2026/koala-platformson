using koala.src.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Cms
{
    // 200 -> 399
    public class CmsErrorCodes : KoalaErrorCodes
    {
        public const int _EXTERNAL_ActiveEditionNotFound = 350;
        public const int _EXTERNAL_EditionNotFound = 351;
    }
    public class CmsException : Exception
    {
        public int ErrorCode { get; }
        public CmsException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class CmsExceptionHandler : IExceptionHandler
    {
        public CmsExceptionHandler() {}

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int statusCode;
            ApiError apiError;

            if (exception is CmsException cmsEx)
            {
                //MAPING FROM EXCEPTION ERROR CODES => STATUS CODES
                statusCode = cmsEx.ErrorCode switch
                {
                    _ => StatusCodes.Status400BadRequest
                };

                apiError = new ApiError(statusCode, cmsEx.Message);
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