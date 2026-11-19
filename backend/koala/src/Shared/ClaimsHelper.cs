using System.Security.Claims;
using koala.src.Modules.Account.Entities;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace koala.src.Shared
{
    public class ClaimsHelper
    {
        public static bool IsAuthenticated(ClaimsPrincipal? claimsPrincipal)
        {
            if (claimsPrincipal == null)
            {
                return false;
            }
            if (claimsPrincipal.Identity == null)
            {
                return false;
            }
            return claimsPrincipal.Identity.IsAuthenticated;
        }
        public static bool IsRoleOrganizationAdmin(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return false;
            }
            if (!claimsPrincipal.IsInRole(RoleNames.NAME_ORGANIZATION_ADMIN))
            {
                return false;
            }
            return true;
        }
        public static bool IsRoleOrganizationEditor(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return false;
            }
            if (!claimsPrincipal.IsInRole(RoleNames.NAME_ORGANIZATION_EDITOR))
            {
                return false;
            }
            return true;
        }
        public static bool IsRoleOrganizationReviewer(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return false;
            }
            if (!claimsPrincipal.IsInRole(RoleNames.NAME_ORGANIZATION_REVIEWER))
            {
                return false;
            }
            return true;
        }
        public static bool IsRoleGuardian(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return false;
            }
            if (!claimsPrincipal.IsInRole(RoleNames.NAME_TEAM_GUARDIAN))
            {
                return false;
            }
            return true;
        }
        public static bool IsRoleCaptain(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return false;
            }
            if (!claimsPrincipal.IsInRole(RoleNames.NAME_TEAM_CAPTAIN))
            {
                return false;
            }
            return true;
        }
        public static Guid GetUserGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return Guid.Empty;
            }
            if (claimsPrincipal!.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal!.FindFirstValue(ClaimTypes.NameIdentifier)!, out var guid) ? guid : Guid.Empty;
        }
        public static Guid GetSessionTokenGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return Guid.Empty;
            }
            if (claimsPrincipal!.FindFirstValue("SessionToken") == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal!.FindFirstValue("SessionToken")!, out var guid) ? guid : Guid.Empty;
        }
        public static Guid GetSessionIdGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if (!IsAuthenticated(claimsPrincipal))
            {
                return Guid.Empty;
            }
            if (claimsPrincipal!.FindFirstValue("SessionId") == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal!.FindFirstValue("SessionId")!, out var guid) ? guid : Guid.Empty;
        }
    }
}
