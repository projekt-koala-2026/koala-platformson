using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace koala.src.Shared
{
    public class PasswordHasher
    {
        private static readonly PasswordHasher<object> _passwordHasher = new();

        public static string Hash(string password)
        {
            return _passwordHasher.HashPassword(null!, password);
        }

        public static (bool Verified, bool NeedsRehash, string? NewHash) VerifyAndMaybeRehash(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                return (false, false, null);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                string newHash = _passwordHasher.HashPassword(null!, providedPassword);
                return (true, true, newHash);
            }

            return (true, false, null);
        }
    }
}