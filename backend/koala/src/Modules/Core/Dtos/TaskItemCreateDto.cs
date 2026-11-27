namespace koala.src.Modules.Core.Dtos
{
    //TODO: poprawic nazwe i upewnic sie ze pola sa dobre
    public record TaskItemCreateDto
    (
        string Name,
        bool IsPrimarySchoolTask,
        bool IsSecondarySchoolTask,
        List<TaskPartCreateDto> TaskPartCreateDtos
    );
}