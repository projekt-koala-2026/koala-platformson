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
        [HttpGet("users")]
        public async Task<IActionResult> AdminPanelGetUsersInfo()
        {
            var users = await _userService.AdminPanelGetUsersInfo();
            return Ok(users);
        }
    }
}