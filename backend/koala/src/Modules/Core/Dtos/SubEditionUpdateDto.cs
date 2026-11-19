namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record SubEditionUpdateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose,
        int Version
    );
}