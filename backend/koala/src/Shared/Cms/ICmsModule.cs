using System.Security.Claims;

namespace koala.src.Shared.Core
{
    public interface ICmsModule
    {
        Task Internal_DeleteKoalicjantIfExistsAsync(Guid id);
        Task Internal_CreateKoalicjantIfNotExistsAsync(Guid id, string NameFirst, string NameLast, string Email);
    }
}
