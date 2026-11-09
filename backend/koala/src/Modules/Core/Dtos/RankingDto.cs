namespace koala.src.Modules.Core.Dtos
{
    public record RankingDto(
        Guid? TeamId,
        string TeamName,
        Guid SubeditionId,
        int TotalScore,
        int MaxScore,
        string Tier,
        DateTime? LatestUpdatedAt
    );
}
