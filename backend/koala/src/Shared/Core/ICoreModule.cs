using System.Security.Claims;

namespace koala.src.Shared.Core
{
    public interface ICoreModule
    {
        Task<_EditionDto?> Internal_GetTheActiveEditionAsync();
        Task<bool> Internal_ExistSchoolAsync(Guid id);
    }
}
