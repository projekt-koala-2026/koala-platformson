namespace koala.src.Modules.Core.Dtos
{
    public record TaskItemUpdateDto
    (
        string Name,
        bool IsPrimarySchoolTask,
        bool IsSecondarySchoolTask
    );
}
