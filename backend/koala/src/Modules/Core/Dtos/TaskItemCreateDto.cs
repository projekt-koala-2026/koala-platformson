namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record TaskItemCreateDto
    (
        string Name,
        string Tier,
        string ContentJson
    );
}