using koala.src.Shared;

namespace koala.src.Modules.Account.Dtos
{
    public record UserListDto
    (
        List<UserDto> Data,
        ApiPagination Pagination
    );
}