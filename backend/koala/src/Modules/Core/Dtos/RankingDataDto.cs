namespace koala.src.Modules.Core.Dtos
{
    public record RankingDataDto(
        Guid Id,
        Guid RankingId,
        string TeamName,
        bool HasQualifiedToNextEtap,
        int PointsAchived,
        int PointsMax,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}
