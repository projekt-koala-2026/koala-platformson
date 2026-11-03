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
        private IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegisterRequestDto requestDto)
        {
            await _userService.RegisterUserAsync(User, requestDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
        {
            await _userService.DeleteUserAsync(User, id);
            return StatusCode(200, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser([FromRoute] Guid id)
        {
            var data = await _userService.GetUserAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<UserDto>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpGet("{id}/declaration")]
        public async Task<IActionResult> GetUserDeclaration([FromRoute] Guid id)
        {
            var data = await _userService.GetUserDeclarationAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<UserDeclarationDto>(true, DateTime.UtcNow, null, null, data));
        }
        [HttpPut("{id}/declaration")]
        public async Task<IActionResult> UpdateUserDeclaration([FromRoute] Guid id, [FromBody] UserDeclarationUpdateDto requestDto)
        {
            var data = await _userService.UpdateUserDeclarationAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<UserDeclarationDto>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] PageQueryDto pageQueryDto, [FromQuery] UserQueryDto userQueryDto)
        {
            (var result, var pagination) = await _userService.GetUsersAsync(User, pageQueryDto, userQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<UserDto>>(true, DateTime.UtcNow, null, pagination, result));
        }
        [Authorize]
        [HttpPut("{id}/name")]
        public async Task<IActionResult> UpdateUserNames([FromRoute] Guid id, [FromBody] UserChangeNamesDto request)
        {
            var data = await _userService.UpdateUserNamesAsync(User, id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<UserDto>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles([FromRoute] Guid id, [FromBody] UserUpdateRolesDto request)
        {
            var data = await _userService.UpdateUserRolesAsync(User, id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<UserDto>(true, DateTime.UtcNow, null, null, data));
        }
        [Authorize]
        [HttpPost("{id}/censor")]
        public async Task<IActionResult> CensorUser([FromRoute] Guid id, [FromBody] UserCensorRequestDto requestDto)
        {
            await _userService.CensorUserAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpPost("{id}/uncensor")]
        public async Task<IActionResult> UnCensorUser([FromRoute] Guid id, [FromBody] UserCensorRequestDto requestDto)
        {
            await _userService.UnCensorUserAsync(User, id, requestDto);
            return StatusCode(200, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

    }
}