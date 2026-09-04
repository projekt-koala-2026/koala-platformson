namespace koala.src.Shared
{
    public record ApiPagination
    (
        int PageNumber,
        int PageSize,
        int TotalNumber
    );
    public record ApiError
    (
        int Code,
        string Message
    );
    public record ApiResponseWraper<T>
    (
        bool Success,
        DateTime TimeStamp,
        ApiError? Error,
        ApiPagination? Pagination,
        T? Data
    );
}