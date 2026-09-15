using koala.src.Shared;

namespace koala.src.Modules.Core.Dtos
{
    public record SubeditionListDto
    (
        List<SubeditionDto> Data,
        ApiPagination Pagination    
    );
}