namespace koala.src.Shared.Core
{
    public record _EditionDto
    (
        Guid Id,
        string Name,
        string State,
        DateTime TeamRegisterOpen,
        DateTime TeamRegisterClose,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );

    public record _SchoolDto
    (
        Guid Id,
        string NameFull,
        string NameShort,
        string State,
        string City,
        string Road,
        string Building,
        string Rspo,
        string Type,
        string Tier,
        string Email,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}