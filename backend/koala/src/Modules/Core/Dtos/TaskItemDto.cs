namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemDto
    (
        Guid Id,
        Guid Etap1SeriesId,
        string Name,
        bool IsPrimarySchoolTask,
        bool IsSecondarySchoolTask,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Version,
        List<TaskPartDto>? TaskPartDtos
    );
}