using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record LinkListDto
    (
        List<LinkDto> Data,
        ApiPagination Pagination
    );
}