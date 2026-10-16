namespace koala.src.Modules.Account.Dtos
{
    public record TeamQueryDto
    (
        Guid? EditionId,
        Guid? SchoolId,
        Guid? CaptainId,
        Guid? AdminId,
        string? Name
    );
}