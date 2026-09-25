using System.Security.Claims;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using StackExchange.Redis;

namespace koala.src.Shared
{
    public static class SharedExtension
    {
        public static IServiceCollection AddSharedModule(this IServiceCollection services, IConfiguration configuration)
        {
            string redisConnStr = configuration.GetConnectionString("Local_Cache_Redis")!;
            var redis = ConnectionMultiplexer.Connect(redisConnStr);
            services.AddSingleton<IConnectionMultiplexer>(redis);
            // 2. Register your custom Cache Service so it can be injected anywhere (including Cookie events)
            services.AddSingleton<ICacheService, CacheService>();
            // 3. Data Protection MUST match Account Module setup exactly
            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "BFF:DataProtectionKeys")
                .SetApplicationName("koala");

            services.AddAuthentication("SessionCookie")
                .AddCookie("SessionCookie", options =>
                {
                    options.Cookie.Name = "koala_auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);

                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async context =>
                        {
                            try
                            {
                                // GET THE IAccountModule 
                                var accountApi = context.HttpContext.RequestServices.GetRequiredService<IAccountModule>();

                                // PASS PRINCIPAL TO IT AND CATCH EXCEPTIONS
                                var session = await accountApi.Internal_ValidateSessionFromPrincipalAsync(context.Principal);

                                if (session == null)
                                {
                                    context.RejectPrincipal();
                                    return;
                                }

                                // REBUILD CLAIM PRINCIPLE
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                                    new Claim("SessionId", session.SessionToken.ToString()),
                                    new Claim("SessionToken", session.SessionToken.ToString())
                                };

                                if (session.UserRoles != null)
                                {
                                    foreach (var role in session.UserRoles)
                                    {
                                        claims.Add(new Claim(ClaimTypes.Role, role));
                                    }
                                }

                                var identity = new ClaimsIdentity(claims, "SessionCookie");
                                context.ReplacePrincipal(new ClaimsPrincipal(identity));
                            }
                            catch
                            {
                                // CATCH INTERNAL ERRORS
                                context.RejectPrincipal();
                            }
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}