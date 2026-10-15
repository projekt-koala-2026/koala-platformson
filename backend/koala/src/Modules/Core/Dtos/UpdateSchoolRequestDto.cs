namespace koala.src.Modules.Core.Dtos
{
    public record UpdateSchoolRequestDto
    (
        string NameFull,
        string NameShort,
        string State,
        string City,
        string Road,
        string Building,
        string Rspo,
        string Type,
        string Email
    );
}