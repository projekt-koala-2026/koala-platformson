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
        public const string BadRequest = "KOALA_BAD_REQUEST";
        public const string Conflict = "KOALA_CONFLICT";

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
        public const string TaskSubmissionNotFound = "KOALA_TASK_SUBMISSION_NOT_FOUND";
    }

    public static class KoalaValidationErrorCodes
    {
        // ==========================================
        // GENERAL
        // ==========================================
        public const string InvalidValue = "KOALA_VALIDATION_INVALID_VALUE";
        public const string ResourceVersionInvalid = "KOALA_VALIDATION_RESOURCE_VERSION_INVALID";

        public const string FileEmpty = "KOALA_VALIDATION_FILE_EMPTY";
        public const string FileToLarge = "KOALA_VALIDATION_FILE_TO_LARGE";
        public const string FileInvalidType = "KOALA_VALIDATION_FILE_INVALID_TYPE";
        public const string FileTitleToLong = "KOALA_VALIDATION_FILE_TITLE_TO_LONG";

        // ==========================================
        // ACCOUNT MODULE
        // ==========================================
        public const string EmailBodyEmpty = "KOALA_VALIDATION_EMAIL_BODY_EMPTY";
        public const string EmailSubjectEmpty = "KOALA_VALIDATION_EMAIL_SUBJECT_EMPTY";
        public const string EmailEmpty = "KOALA_VALIDATION_EMAIL_EMPTY";
        public const string EmailInvalid = "KOALA_VALIDATION_EMAIL_INVALID";
        public const string EmailTooLong = "KOALA_VALIDATION_EMAIL_TOO_LONG";
        public const string PasswordEmpty = "KOALA_VALIDATION_PASSWORD_EMPTY";
        public const string PasswordTooLong = "KOALA_VALIDATION_PASSWORD_TOO_LONG";
        public const string PasswordTooShort = "KOALA_VALIDATION_PASSWORD_TOO_SHORT";
        public const string PasswordLacksCapitalCharacter = "KOALA_VALIDATION_PASSWORD_LACKS_CAPITAL_CHARACTER";
        public const string PasswordLacksSmallCharacter = "KOALA_VALIDATION_PASSWORD_LACKS_SMALL_CHARACTER";
        public const string PasswordLacksNumberCharacter = "KOALA_VALIDATION_PASSWORD_LACKS_NUMBER_CHARACTER";
        public const string PasswordLacksSpecialCharacter = "KOALA_VALIDATION_PASSWORD_LACKS_SPECIAL_CHARACTER";
        public const string RodoNotAccepted = "KOALA_VALIDATION_RODO_TERMS_NOT_ACCPETED";
        public const string RulesNotAccepted = "KOALA_VALIDATION_RULE_TERMS_NOT_ACCEPTED";
        public const string AgeDeclarationInvalid = "KOALA_VALIDATION_AGE_DECLARATION_INVALID";
        public const string NameFirstEmpty = "KOALA_VALIDATION_NAME_FIRST_EMPTY";
        public const string NameFirstInvalidCharacter = "KOALA_VALIDATION_NAME_FIRST_INVALID_CHARACTER";
        public const string NameFirstTooLong = "KOALA_VALIDATION_NAME_FIRST_TOO_LONG";
        public const string NameLastEmpty = "KOALA_VALIDATION_NAME_LAST_EMPTY";
        public const string NameLastInvalidCharacter = "KOALA_VALIDATION_NAME_LAST_INVALID_CHARACTER";
        public const string NameLastTooLong = "KOALA_VALIDATION_NAME_LAST_TOO_LONG";
        public const string RodoStateEmpty = "KOALA_VALIDATION_RODO_STATE_EMPTY";
        public const string RodoStateTooLong = "KOALA_VALIDATION_RODO_STATE_TOO_LONG";
        public const string RodoStateInvalid = "KOALA_VALIDATION_RODO_STATE_INVALID";
        public const string TeamNameEmpty = "KOALA_VALIDATION_TEAM_NAME_EMPTY";
        public const string TeamNameTooLong = "KOALA_VALIDATION_TEAM_NAME_TOO_LONG";
        public const string TeamSchoolIdEmpty = "KOALA_VALIDATION_TEAM_SCHOOL_ID_EMPTY";
        public const string TeamSchoolIdInvalid = "KOALA_VALIDATION_TEAM_SCHOOL_ID_INVALID";
        public const string TeamEditionIdEmpty = "KOALA_VALIDATION_TEAM_EDITION_ID_EMPTY";
        public const string TeamEditionIdInvalid = "KOALA_VALIDATION_TEAM_EDITION_ID_INVALID";
        public const string TeamCaptainIdEmpty = "KOALA_VALIDATION_TEAM_CAPTAIN_ID_EMPTY";
        public const string TeamCaptainIdInvalid = "KOALA_VALIDATION_TEAM_CAPTAIN_ID_INVALID";
        public const string TeamGuardianIdEmpty = "KOALA_VALIDATION_TEAM_GUARDIAN_ID_EMPTY";
        public const string TeamGuardianIdInvalid = "KOALA_VALIDATION_TEAM_GUARDIAN_ID_INVALID";
        public const string TeamMemberIdEmpty = "KOALA_VALIDATION_TEAM_MEMBER_ID_EMPTY";
        public const string TeamMemberIdInvalid = "KOALA_VALIDATION_TEAM_MEMBER_ID_INVALID";
        public const string UserRolesEmpty = "KOALA_VALIDATION_USER_ROLES_EMPTY";
        public const string UserRoleEmpty = "KOALA_VALIDATION_USER_ROLE_EMPTY";
        public const string UserRoleTooLong = "KOALA_VALIDATION_USER_ROLE_TOO_LONG";
        public const string UserRoleInvalid = "KOALA_VALIDATION_USER_ROLE_INVALID";
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
                    KoalaErrorCodes.TaskAnswerNotFound or
                    KoalaErrorCodes.TaskSubmissionNotFound or
                    KoalaErrorCodes.TaskNotFound =>
                        StatusCodes.Status404NotFound,

                    KoalaErrorCodes.ActiveEditionAlreadyExists or
                    KoalaErrorCodes.Conflict or
                    KoalaErrorCodes.ResourceVersionConflict =>
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
