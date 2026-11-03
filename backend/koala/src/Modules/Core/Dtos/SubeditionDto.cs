namespace koala.src.Modules.Core.Dtos
{
    public record SubEditionDto
    (
        Guid Id,
        Guid EditionId,
        string Name,
        DateTime AnswerOpen,
        DateTime AnswerClose,
        string State,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}