namespace koala.src.Shared
{
    public record PageQueryDto
    (
        int PageNumber = 0,
        int PageSize = 16
    );
}