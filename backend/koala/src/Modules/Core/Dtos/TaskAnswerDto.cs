namespace koala.src.Modules.Core.Dtos
{
    public record TaskAnswerDto
    (
        Guid Id,
        Guid TaskItemId,
        string Answer,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version
    );
}