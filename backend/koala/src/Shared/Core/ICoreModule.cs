using System.Security.Claims;

namespace koala.src.Shared.Core
{
    public interface ICoreModule
    {
        Task<_EditionDto?> Internal_GetTheActiveEdition();
    }
}