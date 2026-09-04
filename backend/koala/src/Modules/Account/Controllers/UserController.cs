using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/users")]
    public class UserController : ControllerBase
    {
        private UserService _userService;
        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequestDto requestDto)
        {
            await _userService.RegisterUserAsync(User, requestDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
        {
            await _userService.DeleteUserAsync(User, id);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser([FromRoute] Guid id)
        {
            var data = await _userService.GetUserAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] PageQueryDto pageQueryDto)
        {
            var result = await _userService.GetUsersAsync(User, pageQueryDto); 
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, result.Pagination, result.Data));
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserNames([FromRoute] Guid id, [FromBody] UserChangeNamesDto request)
        {
            var data = await _userService.UpdateUserNamesAsync(User, id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpPost("censor/{id}")]
        public async Task<IActionResult> CensorUser([FromRoute] Guid id)
        {
            await _userService.CensorUserAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpPost("uncensor/{id}")]
        public async Task<IActionResult> UnCensorUser([FromRoute] Guid id)
        {
            await _userService.UnCensorUserAsync(User, id);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }

    }
}