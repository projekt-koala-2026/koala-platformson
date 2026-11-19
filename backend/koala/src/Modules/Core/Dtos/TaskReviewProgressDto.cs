namespace koala.src.Modules.Core.Dtos
{
    public record TaskReviewProgressDto(int TotalSubmissions, int Unassigned, int Incomplete, List<ReviewPairProgressDto> Pairs);
}
