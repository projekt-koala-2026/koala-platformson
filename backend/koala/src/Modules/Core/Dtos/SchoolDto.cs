namespace koala.src.Modules.Core.Dtos
{
    public record SchoolDto
    (
        Guid Id,
        string NameFull,
        string NameShort,
        string State,
        string City,
        string Road,
        string House,
        string Rspo,
        string Type,
        string Email,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}