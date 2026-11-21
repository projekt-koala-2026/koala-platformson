namespace koala.src.Modules.Core.Dtos
{
    public record RankingUpdateDto(
        string Name,
        string Tier,
        int Version
    );
}
