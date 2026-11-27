namespace koala.src.Modules.Core.Dtos
{
    public record TaskPartCreateDto
    (
        string Name,
        string Description,
        int MaxSubmissionLength,
        List<TaskAnswerCreateDto> TaskAnswerCreateDtos
    );
}