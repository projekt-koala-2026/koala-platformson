namespace koala.src.Modules.Core.Dtos
{
    public record TaskPartDto
    (
        Guid Id,
        Guid TaskItemId,
        string Name,
        string Description,
        int MaxSubmissionLength,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version,
        List<TaskAnswerDto>? TaskAnswerDtos
    );
}