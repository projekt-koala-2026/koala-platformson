using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/sessions")]
    public class SessionController : ControllerBase
    {
        private SessionService _sessionService;
        public SessionController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
        {
            var session = await _sessionService.CreateSessionAsync(requestDto);
            
            // REBUILD CLAIM PRINCIPLE
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim("SessionId", session.SessionId.ToString()),
                new Claim("SessionToken", session.SessionToken.ToString())
            };
            if (session.UserRoles != null)
            {
                foreach (var role in session.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, 
                ExpiresUtc = session.ExpiresAt
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            await HttpContext.SignInAsync(
                "SessionCookie", 
                new ClaimsPrincipal(identity), 
                authProperties
            );
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<UserDto>(true, DateTime.UtcNow, null, null, session.User)); 
        } 
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Logout()
        {
            await _sessionService.DeleteSessionAsync(User, ClaimsHelper.GetSessionIdGuid(User));
            await HttpContext.SignOutAsync("SessionCookie");
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetActiveUserSessions([FromQuery] PageQueryDto pageQueryDto)
        {
            var response = await _sessionService.GetSessionsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<SessionDto>>(true, DateTime.UtcNow, null, response.Pagination, response.Data)); 
        }
        [Authorize]
        [HttpDelete("{session_id}")]
        public async Task<IActionResult> DeActivateUserSession([FromRoute] Guid session_id)
        {
            await _sessionService.DeleteSessionAsync(User, session_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpDelete("all")]
        public async Task<IActionResult> DeActivateUserSessions()
        {
            await _sessionService.DeleteSessionsAsync(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

    }
}