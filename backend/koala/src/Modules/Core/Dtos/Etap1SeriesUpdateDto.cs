namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record Etap1SeriesUpdateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose,
        int Version
    );
}