using koala.src.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Cms
{
    // 400 -> 599
    public class CmsErrorCodes : KoalaErrorCodes
    {
        public const int StaticPageNotFound = 400;
        public const int KoalicjantNotFound = 402;
        public const int SponsorNotFound = 403;
        public const int PostNotFound = 404;
        public const int InvalidJsonStructure = 410;
        public const int _EXTERNAL_ActiveEditionNotFound = 550;
        public const int _EXTERNAL_EditionNotFound = 551;
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
                    CmsErrorCodes.FolderCreationError => StatusCodes.Status500InternalServerError,
                    CmsErrorCodes.FileCreationError => StatusCodes.Status500InternalServerError,
                    _ => StatusCodes.Status400BadRequest
                };

                apiError = new ApiError(statusCode, cmsEx.Message);

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
            // else
            // {
            //     statusCode = StatusCodes.Status500InternalServerError;
            //     apiError = new ApiError(statusCode, "Internal server error");
            // }
        }
    }
}