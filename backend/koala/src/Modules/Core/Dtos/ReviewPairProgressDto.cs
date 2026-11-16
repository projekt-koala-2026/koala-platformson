namespace koala.src.Modules.Core.Dtos
{
    public record ReviewPairProgressDto(Guid Reviewer1Id, Guid Reviewer2Id, int Total, int Reviewer1Reviewed, int Reviewer2Reviewed, int Reviewer1Pending, int Reviewer2Pending, int PendingSubmissions);
}
