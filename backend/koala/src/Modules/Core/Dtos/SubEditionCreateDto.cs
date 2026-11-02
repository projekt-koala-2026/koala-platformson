namespace koala.src.Modules.Core.Dtos
{
    public record SubEditionCreateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose
    );
}