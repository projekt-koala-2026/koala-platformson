using koala.src.Shared;

namespace koala.src.Modules.Core.Dtos
{
    public record EditionListDto
    (
        List<EditionDto> Data,
        ApiPagination Pagination    
    );
}