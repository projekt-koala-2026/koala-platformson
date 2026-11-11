namespace koala.src.Modules.Core.Dtos
{
    public record ReviewPairProgressDto(Guid Reviewer1Id, Guid Reviewer2Id, int Total, int Reviewer1Reviewed, int Reviewer2Reviewed, int Reviewer1Pending, int Reviewer2Pending, int PendingSubmissions);
    public record TaskReviewProgressDto(int TotalSubmissions, int Unassigned, int Incomplete, List<ReviewPairProgressDto> Pairs);
    public record PendingReviewerDto(Guid ReviewerId, int Pending, List<Guid> UnavailableReplacements);
    public record AutoCheckProgressDto(int Total, int ReadyToCheck, int MissingKey, int AlreadyReviewedWithoutResult);
}
