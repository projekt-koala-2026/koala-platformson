using System.Security.Claims;

namespace koala.src.Shared.Account
{
    public interface IAccountModule
    {
        Task<_SessionResponseDto?> Internal_ValidateSessionFromPrincipalAsync(ClaimsPrincipal? principal);
        Task Internal_GetUsersByIdsAsync();
    }
}