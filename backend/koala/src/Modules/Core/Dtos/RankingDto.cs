namespace koala.src.Modules.Core.Dtos
{
    public record RankingDto(
        Guid RankingId,
        Guid EditionId,
        bool IsPublic,
        string Name,
        string Tier,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}
