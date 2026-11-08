namespace koala.src.Modules.Core.Dtos
{
    public record RankingDto(
        Guid TeamId,
        int TotalScore,
        int MaxScore,
        string Tier,
        DateTime? LatestUpdatedAt
    );
}