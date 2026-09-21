namespace koala.src.Modules.Cms.Dtos
{
    public record PostQueryDto
    (
        Guid? EditionId,
        string? Name,
        bool? ShowHiden
    );
}