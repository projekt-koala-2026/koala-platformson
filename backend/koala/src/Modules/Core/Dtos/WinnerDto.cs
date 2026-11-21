namespace koala.src.Modules.Core.Dtos
{
    public record WinnerDto(
        Guid Id,
        Guid EditionId,
        string TeamName,
        string Tier,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}
