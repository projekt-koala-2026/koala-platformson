namespace koala.src.Modules.Core.Dtos
{
    public record TaskAnswerDto
    (
        Guid Id,
        Guid TaskId,
        string Answer
    );
}