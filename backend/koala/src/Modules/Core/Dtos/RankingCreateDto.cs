namespace koala.src.Modules.Core.Dtos
{
    public record RankingCreateDto(
        string RankingName,
        string RankingTier,
        List<RankingDataUpsertDto> RankingData
    );
}
