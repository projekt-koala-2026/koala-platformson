using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Cms.Controllers
{
    [ApiController]
    [Route("api/koala/cms/public-files")]
    public class PublicFileController : ControllerBase
    {
        private readonly PublicFileService _publicFileService;

        public PublicFileController(PublicFileService publicFileService)
        {
            _publicFileService = publicFileService;
        }

        [HttpPost]
        public async Task<IActionResult> AddFile([FromForm] string name, [FromForm] IFormFile file)
        {
            var responseData = await _publicFileService.AddFileAsync(User,name,file);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<PublicFileDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile([FromRoute] Guid id)
        {
            await _publicFileService.DeleteFileAsync(User,id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [HttpGet]
        public async Task<IActionResult> GetFiles([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData , var responsePagination) = await _publicFileService.GetFilesAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<PublicFileDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        // [HttpPut("{id}")]
        // public async Task<IActionResult> UpdateFile([FromRoute] Guid id)
        // {
        //     //await _publicFileService.AddFile();
        //     return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        // }
    }
}