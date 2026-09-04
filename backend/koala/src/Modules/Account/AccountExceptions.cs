using koala.src.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace koala.src.Modules.Account
{
    // 200 -> 399
    public class AccountErrorCodes : KoalaErrorCodes
    {
        public const int UserNotFound = 200;
        public const int TeamNotFound = 201;
        public const int LinkNotFound = 202;
        public const int SessionNotFound = 203;
        public const int TeamMemberNotFound = 204;
        public const int TeamJoinCodeNotFound = 205;
        public const int IncorectPassword = 210;
        public const int IncorectRoles = 211;
        public const int UserIsAPartOfTeamAlready = 220;
        public const int TeamMemberAlreadyExists = 221;
        public const int TeamMemberCountMax = 222;
        public const int UserAlreadyExists = 223;
        public const int _EXTERNAL_ActiveEditionNotFound = 350;
    }
    public class AccountException : Exception
    {
        public int ErrorCode { get; }
        public AccountException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class AccountExceptionHandler : IExceptionHandler
    {
        public AccountExceptionHandler() {}

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int statusCode;
            ApiError apiError;

            if (exception is AccountException accountEx)
            {
                //MAPING FROM EXCEPTION ERROR CODES => STATUS CODES
                statusCode = accountEx.ErrorCode switch
                {
                    // 404 Not Found - Standard resource resolution failures
                    AccountErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
                    AccountErrorCodes.TeamNotFound => StatusCodes.Status404NotFound,
                    AccountErrorCodes.LinkNotFound => StatusCodes.Status404NotFound,
                    AccountErrorCodes.TeamMemberNotFound => StatusCodes.Status404NotFound,
                    AccountErrorCodes.TeamJoinCodeNotFound => StatusCodes.Status404NotFound,
                    AccountErrorCodes._EXTERNAL_ActiveEditionNotFound => StatusCodes.Status404NotFound,
                    // 401 Unauthorized - Authentication and credential failures
                    AccountErrorCodes.SessionNotFound => StatusCodes.Status401Unauthorized,
                    AccountErrorCodes.IncorectPassword => StatusCodes.Status401Unauthorized,
                    // 409 Conflict - State violations, duplicates, and capacity limits
                    AccountErrorCodes.UserIsAPartOfTeamAlready => StatusCodes.Status409Conflict,
                    AccountErrorCodes.TeamMemberAlreadyExists => StatusCodes.Status409Conflict,
                    AccountErrorCodes.TeamMemberCountMax => StatusCodes.Status409Conflict,
                    // 400 Bad Request - Payload validation failures (e.g., invalid role input during creation)
                    AccountErrorCodes.IncorectRoles => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status400BadRequest
                };

                apiError = new ApiError(statusCode, accountEx.Message);
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