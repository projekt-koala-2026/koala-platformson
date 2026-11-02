namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemCreateDto
    (
        string Name,
        string Tier,
        string ContentJson
    );
}