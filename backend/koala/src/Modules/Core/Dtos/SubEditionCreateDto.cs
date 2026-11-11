namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record SubEditionCreateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose
    );
}