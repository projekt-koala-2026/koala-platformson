using koala.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using koala.Data.ViewModels;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace koala.Controllers
{
    //FIXME: make sure params are passed corectly (VALIDATION!!!!)
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        public AuthService _authService;
        public ValidationService _validationService;

        public AdminAuthController(AuthService authService, ValidationService validationService)
        {
            _authService = authService;
            _validationService = validationService;
        }
        
        [Authorize(Roles = "ADMIN")]
        [HttpPost("user")]
        public async Task<IActionResult> AdminPanelAddUser([FromBody] UserVM user)
        {
            bool valid_email = _validationService.USERVM_IsEmailValid(user);
            bool valid_password = _validationService.USERVM_IsPasswordValid(user);
            bool valid_roles = await _validationService.USERVM_IsRolesValidAsync(user);
            if(!valid_email)
            {
                return BadRequest("Email not valid");
            }
            if(!valid_password)
            {
                return BadRequest("Password not valid");
            }
            if(!valid_roles)
            {
                return BadRequest("Roles not valid");
            }
            var added_user = await _authService.AdminPanelAddUser(user);
            if( added_user == null)
            {
                return BadRequest("User already exists");
            }
            return Ok(added_user);
        }

        [AllowAnonymous]
        [HttpPost("session")]
        public async Task<IActionResult> AdminPanelLogin([FromBody] UserVM user)
        {
            bool valid_user = _validationService.USERVM_IsAnyFieldEmpty(user);
            if(!valid_user)
            {
                return BadRequest(new UserVM());
            } 
            var (tokenValue, ruser) = await _authService.AdminPanelLogin(user);
            
            if(string.IsNullOrEmpty(tokenValue))
            {
                return BadRequest(ruser);
            }
            Response.Cookies.Append("KOALA_auth_token", tokenValue, new CookieOptions
            {
                HttpOnly = true,                //TODO: change to true
                Secure = true,                  //TODO: change to true
                SameSite = SameSiteMode.Strict, //TODO: change to SameSiteMode.Strict
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });
            return Ok(ruser);
        }

        [AllowAnonymous]
        [HttpDelete("session")]
        public async Task<IActionResult> AdminPanelLogout()
        {
            var tokenValue = Request.Cookies["KOALA_auth_token"];
            if(string.IsNullOrEmpty(tokenValue))
            {
                return NotFound("No session found");
            }
            await _authService.AdminPanelLogout(tokenValue);
            Response.Cookies.Delete("KOALA_auth_token", new CookieOptions
            {
                HttpOnly = true,                //TODO: change to true
                Secure = true,                  //TODO: change to true
                SameSite = SameSiteMode.Strict, //TODO: change to SameSiteMode.Strict
                Expires = DateTimeOffset.UtcNow.AddHours(0),
                Path = "/",
            });
            return Ok("Succesful logout");
        }
    }
}
