using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Cms.Controllers
{
    [ApiController]
    [Route("api/koala/cms/posts")]
    public class PostController : ControllerBase
    {
        private readonly PostService _postService;

        public PostController(PostService postService)
        {
            _postService = postService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequestDto requestDto)
        {
            var responseData = await _postService.CreatePostAsync(User, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<PostDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost([FromRoute] Guid id)
        {
            await _postService.DeletePostAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> CreatePost([FromRoute] Guid id, [FromBody] UpdatePostRequestDto requestDto)
        {
            var responseData = await _postService.UpdatePostAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<PostDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _postService.GetPostsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<PostDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }
    }
}