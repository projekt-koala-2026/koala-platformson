namespace koala.src.Modules.Core.Dtos
{
    public record SubEditionUpdateDto
    (
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose,
        int Version
    );
}