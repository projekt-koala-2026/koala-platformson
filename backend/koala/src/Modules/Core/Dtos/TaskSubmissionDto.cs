namespace koala.src.Modules.Core.Dtos
{
    public record TaskSubmissionDto
    (
        Guid TeamId,
        Guid TaskId,
        Guid? Reviewer1Id,
        Guid? Reviewer2Id,
        bool? AutoReviewState,
        bool? Review1Score,
        bool? Review2Score,
        string Submission,
        string? State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}