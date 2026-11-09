using System.Security.Claims;

namespace koala.src.Shared.Account
{
    public interface IAccountModule
    {
        Task<_SessionResponseDto?> Internal_ValidateSessionFromPrincipalAsync(ClaimsPrincipal? principal);
        Task<_TeamResponseDto?> Internal_GetTeamInfo(Guid team_id);
        Task<List<_TeamResponseDto>> Internal_GetTeamsForEditionAsync(Guid editionId);
        Task<List<Guid>> Internal_GetReviewerIdsAsync(List<Guid> userIds);
    }
}
