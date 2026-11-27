namespace koala.src.Modules.Core.Dtos
{
    public record EditionDto
    (
        Guid Id,
        string Name,
        string State,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool HasTaskFile,
        DateTime? TaskFileTimeWindowOpen,
        DateTime? TaskFileTimeWindowClose,
        int Version
    );
}
