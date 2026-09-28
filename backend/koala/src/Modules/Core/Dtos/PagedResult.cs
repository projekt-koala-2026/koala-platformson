using koala.src.Shared;

namespace koala.src.Modules.Core.Dtos;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public ApiPagination Pagination { get; set; } = default!;

    public PagedResult(List<T> items, ApiPagination pagination)
    {
        Items = items;
        Pagination = pagination;
    }
}