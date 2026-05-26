using koala.Services;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace koala.Controllers
{
    //FIXME: make sure params are passed corectly (VALIDATION!!!!)
    [ApiController]
    [Route("api/admin/post")]
    public class PostController : ControllerBase
    {
        public readonly PostService _postService;

        public PostController(PostService postService)
        {
            _postService = postService;
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] PostCreateVM newPost)
        {
            // [ApiController] attribute actually handles the basic ModelSate check for you,
            // but keeping this here is fine for explicit validation.
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _postService.CreatePostAsync(newPost);
            return CreatedAtAction(nameof(GetPostById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPut("{id}")]
        public async Task<IActionResult> EditPost(Guid id, [FromBody] PostUpdateVM updatedPost)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _postService.UpdatePostAsync(id, updatedPost);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var success = await _postService.DeletePostAsync(id);
            return success ? Ok(new { message = "Deleted" }) : NotFound();
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(Guid id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            return post == null ? NotFound() : Ok(post);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var posts = await _postService.GetAllPostsAsync();
            return Ok(posts);
        }
    }
}
