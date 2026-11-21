namespace koala.src.Modules.Core.Dtos
{
    public record RankingDataUpsertDto(
        string TeamName,
        int PointsAchived,
        int PointsMax,
        int? Version
    );
}
