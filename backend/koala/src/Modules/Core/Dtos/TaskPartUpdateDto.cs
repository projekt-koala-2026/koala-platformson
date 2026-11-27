namespace koala.src.Modules.Core.Dtos
{
    public record TaskPartUpdateDto
    (
        string Name,
        string Description,
        int MaxSubmissionLength
    );
}