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
        public ValidationService _validationService;

        public AdminUserController(UserService userService, ValidationService validationService)
        {
            _userService = userService;
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
            var added_user = await _userService.AdminPanelAddUser(user);
            if( added_user == null)
            {
                return BadRequest("User already exists");
            }
            return Ok(added_user);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPut("user")]
        public async Task<IActionResult> AdminPanelChangeUser([FromBody] UserVM user)
        {
            bool valid_email = _validationService.USERVM_IsEmailValid(user);
            bool valid_password = _validationService.USERVM_IsPasswordValid(user);
            bool notEmpty_email = _validationService.USERVM_IsEmailNotEmpty(user);
            bool notEmpty_password = _validationService.USERVM_IsPasswordNotEmpty(user);
            if(!notEmpty_email && !notEmpty_password)
            {
                return BadRequest("Empty data");
            }
            if(notEmpty_email && !valid_email)
            {
                return BadRequest("Email data inavlid");
            }
            if(notEmpty_password && !valid_password)
            {
                return BadRequest("Password data inavlid");
            }
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                var changed_user = await _userService.AdminPanelChangeUser(user,userId);
                if(changed_user == null)
                {
                    return BadRequest("Could not change the user data");
                }
                return Ok(changed_user);
            }
            return BadRequest("Incorect session");
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("user")]
        public async Task<IActionResult> AdminPanelDeleteUser([FromBody] UserVM user)
        {
            bool notEmpty_email = _validationService.USERVM_IsEmailNotEmpty(user);
            if(!notEmpty_email)
            {
                return BadRequest("Email data inavlid");
            }
            await _userService.AdminPanelDeleteUser(user);
            return Ok();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("roles")]
        public async Task<IActionResult> AdminPanelChangeUserRoles([FromBody] UserVM user)
        {
            bool notEmpty_email = _validationService.USERVM_IsEmailNotEmpty(user);
            bool valid_roles = await _validationService.USERVM_IsRolesValidAsync(user);
            if(!notEmpty_email)
            {
                return BadRequest("Email data inavlid");
            }
            if(!valid_roles)
            {
                return BadRequest("Roles not valid");
            }
            var ruser = await _userService.AdminPanelChangeUserRoles(user);
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