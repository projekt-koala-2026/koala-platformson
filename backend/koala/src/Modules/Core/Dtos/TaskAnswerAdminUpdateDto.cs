namespace koala.src.Modules.Core.Dtos
{
    public record UpdateTaskAnswerAdminDto
    (
        Guid TeamId,
        Guid TaskId,
        Guid? Reviewer1Id,
        Guid? Reviewer2Id,
        bool? AutoReviewState,
        bool? Review1State,
        bool? Review2State,
        string TeamAnswer,
        int Version
    );
}