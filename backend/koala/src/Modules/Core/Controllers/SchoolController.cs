using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Core.Controllers
{
    [ApiController]
    [Route("api/koala/core/schools")]
    public class SchoolController : ControllerBase
    {
        private readonly SchoolService _schoolService;

        public SchoolController(SchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequestDto createSchoolRequestDto)
        {
            var response = await _schoolService.CreateSchoolAsync(User, createSchoolRequestDto);
            return StatusCode(200, new ApiResponseWraper<SchoolDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{id}/name")]
        public async Task<IActionResult> UpdateSchool([FromRoute] Guid id, [FromBody] UpdateSchoolRequestDto updateSchoolRequestDto)
        {
            var response = await _schoolService.UpdateSchoolAsync(User, id, updateSchoolRequestDto);
            return StatusCode(200, new ApiResponseWraper<SchoolDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchool([FromRoute] Guid id)
        {
            await _schoolService.DeleteSchoolAsync(User, id);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));                
        }

        [AllowAnonymous]
        [HttpDelete]
        public async Task<IActionResult> DeleteSchools([FromBody] List<Guid> ids)
        {
            await _schoolService.DeleteSchoolsAsync(User, ids);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));                
        }
        
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchool([FromRoute] Guid id)
        {
            var response = await _schoolService.GetSchoolAsync(User, id);
            return StatusCode(200, new ApiResponseWraper<SchoolDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSchools([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _schoolService.GetSchoolsAsync(User, pageQueryDto);
            return StatusCode(200, new ApiResponseWraper<List<SchoolDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }

        [AllowAnonymous]
        [HttpPost("import")]
        public async Task<IActionResult> ImportSchools([FromForm] ImportSchoolRequestDto importSchoolRequestDto)
        {
            var response = await _schoolService.ImportSchoolsAsync(User, importSchoolRequestDto);
            return StatusCode(200, new ApiResponseWraper<int>(true, DateTime.UtcNow, null, null, response));                
        }

    }
}