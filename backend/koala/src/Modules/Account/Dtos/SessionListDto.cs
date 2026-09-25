using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record SessionListDto
    (
        List<SessionDto> Data,
        ApiPagination Pagination
    );
}