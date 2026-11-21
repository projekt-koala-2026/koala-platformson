namespace koala.src.Modules.Core.Dtos
{
    public record RankingMergeRequestDto(
        string MergeRankingName,
        Guid Ranking1Id,
        Guid Ranking2Id
    );
}
