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
    [Route("api/admin/[controller]")]
    public class AuthController : ControllerBase
    {
        public AuthServices _authServices;
        public ValidationService _validationService;

        public AuthController(AuthServices authServices, ValidationService validationService)
        {
            _authServices = authServices;
            _validationService = validationService;
        }
        
        [Authorize(Roles = "ADMIN")]
        [HttpPost("user")]
        public async Task<IActionResult> AdminPanelAddUser([FromBody] UserVM user)
        {
            bool valid_email = _validationService.ValidateUserVMEmail(user);
            bool valid_password = _validationService.ValidateUserVMPassword(user);
            bool valid_roles = await _validationService.ValidateUserVMRoles(user);
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
            var added_user = await _authServices.AdminPanelAddUser(user);
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
            bool valid_email = _validationService.ValidateUserVMEmail(user);
            bool valid_password = _validationService.ValidateUserVMPassword(user);
            if(!valid_email)
            {
                return BadRequest("Email not valid");
            }
            if(!valid_password)
            {
                return BadRequest("Password not valid");
            }
            
            var tokenValue = await _authServices.AdminPanelLogin(user);
            
            if(string.IsNullOrEmpty(tokenValue))
            {
                return NotFound("User not found");
            }
            Response.Cookies.Append("KOALA_auth_token", tokenValue, new CookieOptions
            {
                HttpOnly = true,                //TODO: change to true
                Secure = true,                  //TODO: change to true
                SameSite = SameSiteMode.Strict, //TODO: change to SameSiteMode.Strict
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });
            return Ok("Succesful login");
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
            await _authServices.AdminPanelLogout(tokenValue);
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

        [Authorize(Roles = "ADMIN")]
        [HttpGet("users")]
        public async Task<ActionResult<List<UserVM>>> UserList()
        {
            var users = await _authServices.UserList(); 
            return Ok(users);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("roles")]
        public async Task<ActionResult<List<string>>> RoleList()
        {
            var roles = await _authServices.RoleList(); 
            return Ok(roles);
        }
    }
}
