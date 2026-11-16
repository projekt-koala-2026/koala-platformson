namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record SchoolUpdateRequestDto
    (
        string NameFull,
        string NameShort,
        string State,
        string City,
        string Road,
        string Building,
        string Rspo,
        string Type,
        string Email,
        int Version
    );
}