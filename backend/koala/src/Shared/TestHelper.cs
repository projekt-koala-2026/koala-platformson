using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Shared
{
    public class TestHelper
    {
        public static async Task<ClaimsPrincipal> GenerateTestFilledClaimsPrincipalAsync(Guid userId, Guid sessionId, Guid token, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("SessionId", sessionId.ToString()),
                new Claim("SessionToken", token.ToString()),
            };
            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));    
            }
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            return new ClaimsPrincipal(identity);
        }

        public static async Task<ClaimsPrincipal> GenerateTestEmptyClaimsPrincipalAsync()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}