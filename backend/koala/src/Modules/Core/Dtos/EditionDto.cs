namespace koala.src.Modules.Core.Dtos
{
    public record EditionDto
    (
        Guid Id,
        string Name,
        string State,
        bool AreWinnersPublic,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}