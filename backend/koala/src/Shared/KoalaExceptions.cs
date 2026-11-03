using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using FluentValidation;
using System.Linq;

namespace koala.src.Shared
{
    // ==========================================
    // ERROR CODES
    // ==========================================

    public static class KoalaErrorCodes
    {
        // ==========================================
        // VALIDATION
        // ==========================================
        public const string Validation = "KOALA_VALIDATION";

        // ==========================================
        // AUTH
        // ==========================================
        public const string Unauthenticated = "KOALA_UNAUTHENTICATED";
        public const string Forbidden = "KOALA_FORBIDDEN";
        public const string InvalidCredentials = "KOALA_INVALID_CREDENTIALS";

        // ==========================================
        // FILE SYSTEM
        // ==========================================
        public const string FileNotFound = "KOALA_FILE_NOT_FOUND";
        public const string FileCreationFailure = "KOALA_FILE_CREATION_FAILURE";
        public const string FileReadFailure = "KOALA_FILE_READ_FAILURE";
        public const string FolderCreationFailure = "KOALA_FOLDER_CREATION_FAILURE";

        // ==========================================
        // DATABASE
        // ==========================================
        public const string DatabaseError = "KOALA_DATABASE_ERROR";

        // ==========================================
        // GENERAL
        // ==========================================
        public const string ResourceVersionConflict = "KOALA_RESOURCE_VERSION_CONFLICT";
        public const string InvalidJson = "KOALA_INVALID_JSON";

        // ==========================================
        // ACCOUNT MODULE
        // ==========================================
        public const string UserInvalidRoles = "KOALA_USER_INVALID_ROLES";
        public const string UserRoleAgeRequirementNotMet = "KOALA_USER_ROLE_AGE_REQUIREMENT_NOT_MET";
        public const string UserNotFound = "KOALA_USER_NOT_FOUND";
        public const string UserAlreadyExists = "KOALA_USER_ALREADY_EXISTS";


        public const string UserIsCaptainOfTeamAlready = "KOALA_USER_IS_CAPTAIN_OF_TEAM_ALREADY";
        public const string UserNotPartOfThisTeam = "KOALA_USER_NOT_PART_OF_THIS_TEAM";
        public const string UserNotCaptainOfThisTeam = "KOALA_USER_NOT_CAPTAIN_OF_THIS_TEAM";
        public const string UserNotGuardianOfThisTeam = "KOALA_USER_NOT_GUARDIAN_OF_THIS_TEAM";

        public const string TeamNotFound = "KOALA_TEAM_NOT_FOUND";

        public const string TeamMemberNotFound = "KOALA_TEAM_MEMBER_NOT_FOUND";
        public const string TeamMemberRodoAlreadyExists = "KOALA_TEAM_MEMBER_RODO_ALREADY_EXISTS";
        public const string TeamMemberRodoNotFound = "KOALA_TEAM_MEMBER_RODO_NOT_FOUND";
        public const string TeamMemberAlreadyExists = "KOALA_TEAM_MEMBER_ALREADY_EXISTS";
        public const string TeamMemberLimitExceeded = "KOALA_TEAM_MEMBER_LIMIT_EXCEEDED";

        public const string TeamChangesWindowClosed = "KOALA_TEAM_CHANGES_WINDOW_CLOSED";
        public const string TeamCreationWindowClosed = "KOALA_TEAM_CREATION_WINDOW_CLOSED";

        public const string TeamJoinCodeNotFound = "KOALA_TEAM_JOIN_CODE_NOT_FOUND";

        public const string LinkNotFound = "KOALA_LINK_NOT_FOUND";
        public const string SessionNotFound = "KOALA_SESSION_NOT_FOUND";
        public const string RodoNotFound = "KOALA_RODO_NOT_FOUND";

        // ==========================================
        // CMS MODULE
        // ==========================================
        public const string StaticPageNotFound = "KOALA_STATIC_PAGE_NOT_FOUND";
        public const string KoalicjantNotFound = "KOALA_KOALICJANT_NOT_FOUND";
        public const string SponsorNotFound = "KOALA_SPONSOR_NOT_FOUND";
        public const string PostNotFound = "KOALA_POST_NOT_FOUND";

        // ==========================================
        // CORE MODULE
        // ==========================================
        public const string EditionNotFound = "KOALA_EDITION_NOT_FOUND";
        public const string EditionInvalidStates = "KOALA_EDITION_INVALID_STATES";
        public const string EditionAlreadyExists = "KOALA_EDITION_ALREADY_EXISTS";

        public const string ActiveEditionNotFound = "KOALA_ACTIVE_EDITION_NOT_FOUND";
        public const string ActiveEditionAlreadyExists = "KOALA_ACTIVE_EDITION_ALREADY_EXISTS";

        public const string SubEditionNotFound = "KOALA_SUB_EDITION_NOT_FOUND";
        public const string SubEditionAlreadyExists = "KOALA_SUB_EDITION_ALREADY_EXISTS";

        public const string ActiveSubEditionNotFound = "KOALA_ACTIVE_SUB_EDITION_NOT_FOUND";
        public const string ActiveSubEditionAlreadyExists = "KOALA_ACTIVE_SUB_EDITION_ALREADY_EXISTS";

        public const string SchoolNotFound = "KOALA_SCHOOL_NOT_FOUND";
        public const string SchoolAlreadyExists = "KOALA_SCHOOL_ALREADY_EXISTS";

        public const string TaskNotFound = "KOALA_TASK_NOT_FOUND";
        public const string TaskAlreadyExists = "KOALA_TASK_ALREADY_EXISTS";

        public const string TaskAnswerNotFound = "KOALA_TASK_ANSWER_NOT_FOUND";
        public const string TaskAnswerAlreadyExists = "KOALA_TASK_ANSWER_ALREADY_EXISTS";
    }

    public static class KoalaValidationErrorCodes
    {
        // General
        public const string InvalidValue = "KOALA_INVALID_VALUE";

        // Edition
        public const string EditionNameRequired = "KOALA_EDITION_NAME_REQUIRED";
        public const string EditionNameTooLong = "KOALA_EDITION_NAME_TOO_LONG";

        // Task
        public const string TaskNameRequired = "KOALA_TASK_NAME_REQUIRED";
        public const string TaskNameTooLong = "KOALA_TASK_NAME_TOO_LONG";
    }

    public class KoalaProblems : ProblemDetails
    {
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public IDictionary<string, string[]>? Errors { get; set; }
    }

    public class KoalaException : Exception
    {
        public string ErrorCode { get; }

        public KoalaException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class KoalaExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync
        (
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken
        )
        {
            int statusCode;
            KoalaProblems problem;

            // ==========================================
            // FLUENT VALIDATION
            // ==========================================
            if (exception is FluentValidation.ValidationException validationException)
            {
                statusCode = StatusCodes.Status400BadRequest;

                var errors = validationException.Errors
                    .Select(error => new
                    {
                        Field = error.PropertyName,
                        ErrorCode = string.IsNullOrWhiteSpace(error.ErrorCode) ? KoalaValidationErrorCodes.InvalidValue : error.ErrorCode
                    })
                    .GroupBy(error => error.Field)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorCode).ToArray()
                    );

                problem = new KoalaProblems
                {
                    Type = "about:blank",
                    Title = ReasonPhrases.GetReasonPhrase(statusCode),
                    Status = statusCode,
                    Detail = "One or more validation errors occurred.",
                    Instance = httpContext.Request.Path,
                    ErrorCode = KoalaErrorCodes.Validation,
                    Timestamp = DateTime.UtcNow,
                    Errors = errors
                };

                httpContext.Response.StatusCode = statusCode;
                httpContext.Response.ContentType = "application/problem+json";

                await httpContext.Response.WriteAsJsonAsync(
                    problem,
                    cancellationToken
                );

                return true;
            }

            // ==========================================
            // NORMAL KOALA EXCEPTIONS
            // ==========================================
            if (exception is KoalaException koalaException)
            {
                statusCode = koalaException.ErrorCode switch
                {
                    KoalaErrorCodes.Unauthenticated =>
                        StatusCodes.Status401Unauthorized,

                    KoalaErrorCodes.Forbidden =>
                        StatusCodes.Status403Forbidden,

                    KoalaErrorCodes.FileNotFound or
                    KoalaErrorCodes.EditionNotFound or
                    KoalaErrorCodes.ActiveEditionNotFound or
                    KoalaErrorCodes.SubEditionNotFound or
                    KoalaErrorCodes.TaskNotFound =>
                        StatusCodes.Status404NotFound,

                    KoalaErrorCodes.ActiveEditionAlreadyExists =>
                        StatusCodes.Status409Conflict,

                    _ => StatusCodes.Status400BadRequest
                };

                problem = new KoalaProblems
                {
                    Type = "about:blank",
                    Title = ReasonPhrases.GetReasonPhrase(statusCode),
                    Status = statusCode,
                    Detail = koalaException.Message,
                    Instance = httpContext.Request.Path,
                    ErrorCode = koalaException.ErrorCode,
                    Timestamp = DateTime.UtcNow
                };

                httpContext.Response.StatusCode = statusCode;
                httpContext.Response.ContentType = "application/problem+json";

                await httpContext.Response.WriteAsJsonAsync(
                    problem,
                    cancellationToken
                );

                return true;
            }

            return false;
        }
    }
}