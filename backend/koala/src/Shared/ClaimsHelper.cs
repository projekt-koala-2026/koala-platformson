using System.Security.Claims;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace koala.src.Shared
{
    public class ClaimsHelper
    {
        public static bool IsAuthenticated(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.Identity.IsAuthenticated)
            {
                return false;
            }
            return true;
        }
        public static bool IsOrganizationAdmin(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.IsInRole("ORGANIZATION_ADMIN"))
            {
                return false;
            }
            return true;
        }
        public static bool IsOrganizationEditor(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.IsInRole("ORGANIZATION_EDITOR"))
            {
                return false;
            }
            return true;
        }
        public static bool IsOrganizationReviuer(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.IsInRole("ORGANIZATION_REVIUER"))
            {
                return false;
            }
            return true;
        }
        public static bool IsTeamAdmin(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.IsInRole("TEAM_ADMIN"))
            {
                return false;
            }
            return true;
        }
        public static bool IsTeamPlayer(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return false;
            }
            if(!claimsPrincipal.IsInRole("TEAM_PLAYER"))
            {
                return false;
            }
            return true;
        }
        public static Guid GetUserGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return Guid.Empty;
            }
            if(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!, out var guid) ? guid : Guid.Empty;
        }
        public static Guid GetSessionTokenGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return Guid.Empty;
            }
            if(claimsPrincipal.FindFirstValue("SessionToken") == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal.FindFirstValue("SessionToken")!, out var guid) ? guid : Guid.Empty;
        }
        public static Guid GetSessionIdGuid(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal == null)
            {
                return Guid.Empty;
            }
            if(claimsPrincipal.FindFirstValue("SessionId") == null)
            {
                return Guid.Empty;
            }
            return Guid.TryParse(claimsPrincipal.FindFirstValue("SessionId")!, out var guid) ? guid : Guid.Empty;
        }
    }
}