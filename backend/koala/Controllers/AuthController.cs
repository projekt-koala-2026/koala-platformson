using koala.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using koala.Data.ViewModels;

namespace koala.Controllers
{
    //FIXME: mkae sure params are passed corectly (VALIDATION!!!!)
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public AuthServices _authServices;

        public AuthController(AuthServices authServices)
        {
            _authServices = authServices;
        }

        [HttpPost("user")]
        public async Task<IActionResult> AdminPanelAddUser([FromBody] UserVM user)
        {
            await _authServices.AdminPanelAddUser(user);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> AdminPanelLogin([FromBody] UserVM user)
        {
            var tokenValue = await _authServices.AdminPanelLogin(user);
            if(tokenValue == "")
            {
                return NotFound("User not found");
            }
            Response.Cookies.Append("auth_token", tokenValue, new CookieOptions
            {
                HttpOnly = true,                //TODO: change to true
                Secure = true,                  //TODO: change to true
                SameSite = SameSiteMode.Strict, //TODO: change to SameSiteMode.Strict
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });
            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> AdminPanelLogout()
        {
            var token = Request.Cookies["auth_token"];
            Console.WriteLine($"{token}");
            await _authServices.AdminPanelLogout(token);
            return Ok();
        }

        [HttpGet("list")]
        public async Task<List<UserVM>> List()
        {
            var token = Request.Cookies["auth_token"];
            Console.WriteLine($"{token}");
            return await _authServices.List();
        }
    }
}
