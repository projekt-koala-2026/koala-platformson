namespace koala.src.Modules.Account.Dtos
{
    public record TeamQueryDto
    (
        Guid? EditionId,
        Guid? SchoolId,
        string? Name
    );
}