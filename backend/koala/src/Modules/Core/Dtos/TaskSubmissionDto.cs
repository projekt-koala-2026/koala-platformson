namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
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