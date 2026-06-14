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
    [Route("api/admin/user")]
    public class AdminUserController : ControllerBase
    {
        public UserService _userService;
        public KoalicjantService _koalicjantService;

        public AdminUserController(UserService userService, KoalicjantService koalicjantService)
        {
            _userService = userService;
            _koalicjantService = koalicjantService;
        }

        [AllowAnonymous]
        [HttpPost("create-account")]
        public async Task<IActionResult> CreateNormalUser([FromBody] UserCreateNormalVM user)
        {
            var added_user = await _userService.CreateNormalUser(user);
            if( added_user == null)
            {
                return BadRequest("User already exists");
            }
            return Ok(added_user);
        }


        [Authorize(Roles = "ADMIN")]
        [HttpPost("user")]
        public async Task<IActionResult> AdminPanelAddUser([FromBody] UserCreateVM user)
        {
            var added_user = await _userService.AdminPanelAddUser(user);
            if( added_user == null)
            {
                return BadRequest("User already exists");
            }
            await _koalicjantService.AutoCreateAsync(user.Email);
            return Ok(added_user);
        }

        [Authorize(Roles = "ADMIN,EDITOR,CAPTAIN")]
        [HttpPut("email")]
        public async Task<IActionResult> AdminPanelChangeUserEmail([FromBody] UserChangeEmailVM userChangeEmailVM)
        {
            var changed_user = await _userService.AdminPanelChangeUserEmail(userChangeEmailVM);
            if(changed_user == null)
            {
                return BadRequest("Could not change the user data");
            }
            return Ok(changed_user);
        }

        [Authorize(Roles = "ADMIN,EDITOR,CAPTAIN")]
        [HttpPut("password")]
        public async Task<IActionResult> AdminPanelChangeUserPassword([FromBody] UserChangePasswordVM userChangePasswordVM)
        {
            var changed_user = await _userService.AdminPanelChangeUserPassword(userChangePasswordVM);
            if(changed_user == null)
            {
                return BadRequest("Could not change the user data");
            }
            return Ok(changed_user);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("user")]
        public async Task<IActionResult> AdminPanelDeleteUser([FromBody] UserDeleteVM userDeleteVM)
        {
            await _userService.AdminPanelDeleteUser(userDeleteVM);
            return Ok();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("roles")]
        public async Task<IActionResult> AdminPanelChangeUserRoles([FromBody] UserChangeRolesVM userChangeRolesVM)
        {
            var ruser = await _userService.AdminPanelChangeUserRoles(userChangeRolesVM);
            return Ok(ruser);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("users")]
        public async Task<IActionResult> AdminPanelGetUsersInfo()
        {
            var users = await _userService.AdminPanelGetUsersInfo();
            return Ok(users);
        }
    }
}