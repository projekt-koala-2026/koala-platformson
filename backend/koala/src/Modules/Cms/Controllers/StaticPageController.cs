using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Cms.Controllers
{
    [ApiController]
    [Route("api/koala/cms/static-pages")]
    public class StaticPageController : ControllerBase
    {
        private readonly StaticPageService _staticPageService;

        public StaticPageController(StaticPageService staticPageService)
        {
            _staticPageService = staticPageService;
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaticPage([FromRoute] Guid id, [FromBody] UpdateStaticPageRequestDto requestDto)
        {
            var responseData = await _staticPageService.UpdateStaticPagesAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<StaticPageDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetStaticPages()
        {
            var responseData = await _staticPageService.GetStaticPagesAsync(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<StaticPageDto>>(true, DateTime.UtcNow, null, null, responseData));
        }
    }
}