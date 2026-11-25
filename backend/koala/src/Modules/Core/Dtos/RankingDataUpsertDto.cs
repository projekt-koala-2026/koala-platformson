namespace koala.src.Modules.Core.Dtos
{
    public record RankingDataUpsertDto(
        string TeamName,
        bool HasQualifiedToNextEtap,
        int PointsAchived,
        int PointsMax,
        int? Version
    );
}
