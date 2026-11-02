using System.Security.Claims;

namespace koala.src.Shared.Core
{
    public interface ICoreModule
    {
        Task<_EditionDto?> Internal_GetTheActiveEditionAsync();
        Task<_SchoolDto?> Internal_GetSchoolAsync(Guid id);
        Task<(List<string>, ApiPagination)> Internal_GetSchoolsEmailsAsync(PageQueryDto pageQueryDto);
    }
}
