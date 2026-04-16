using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using koala.Data;

public class KoalaAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public KoalaAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IDbContextFactory<AppDbContext> factory)
        : base(options, logger, encoder, clock)
    {
        _factory = factory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Cookies["KOALA_auth_token"];

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        using var context = await _factory.CreateDbContextAsync();

        //NOTE: IS TOKEN VALID
        var session = await context.Tokens
            .FirstOrDefaultAsync(t => t.Value == token);

        if (session == null)
        {
            return AuthenticateResult.Fail("Invalid session");
        }

        //NOTE: IS TOKEN EXPIRED

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("Session Expired");
        }

        //NOTE: GET THE USER TO WHOM THE TOKEN BELONGS
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == session.UserId);

        if (user == null)
        {
            return AuthenticateResult.Fail("User not found");
        }

        //NOTE: LOAD THE USER ROLES
        var roles = await context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Value)
            .ToListAsync();

        //NOTE: CREATE THE CLAIMS FOR .NET RUN TIME
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}