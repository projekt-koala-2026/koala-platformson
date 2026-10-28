namespace koala.src.Modules.Core.DTOs
{
    public record TaskAnswerDto(
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