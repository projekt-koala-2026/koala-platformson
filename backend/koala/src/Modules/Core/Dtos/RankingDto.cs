namespace koala.src.Modules.Core.Dtos
{
    public record RankingDto(
        Guid TeamId, 
        int TotalScore,
        DateTime? LatestUpdatedAt
    );
}