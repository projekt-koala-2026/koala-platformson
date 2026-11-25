namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record Etap1SeriesCreateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose
    );
}