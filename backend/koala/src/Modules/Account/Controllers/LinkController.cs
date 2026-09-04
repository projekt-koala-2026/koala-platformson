using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/links")]
    public class LinkController : ControllerBase
    {
        private LinkService _linkService;
        public LinkController(LinkService linkService)
        {
            _linkService = linkService;
        }
        [AllowAnonymous]
        [HttpPost("register/{token}")]
        public async Task<IActionResult> LinkRegisterAccount([FromRoute] Guid token, [FromBody] ConsumeRegisterLinkRequestDto requestDto)
        {
            await _linkService.ConsumeRegisterLink(token, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [AllowAnonymous]
        [HttpPost("reset-password/{token}")]
        public async Task<IActionResult> LinkResetPasswordAccount([FromRoute] Guid token, [FromBody]ConsumeResetPasswordLinkRequestDto requestDto)
        {
            await _linkService.ConsumeResetPasswordLink(token, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> CreateResetPasswordLink([FromBody] GenerateResetPasswordLinkRequestDto requestDto)
        {
            await _linkService.GenerateResetPasswordLink(requestDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetActiveUserLinks([FromQuery] PageQueryDto pageQueryDto)
        {
            var response = await _linkService.GetActiveUserLinks(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, response.Pagination, response.Data));
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserLink([FromRoute] Guid id)
        {
            await _linkService.DeleteUserLink(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<LinkListDto>(true, DateTime.UtcNow, null, null, null));
        }
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteUserLinks()
        {
            await _linkService.DeleteUserLinks(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }
    }
}