using koala.src.Modules.Account.Entities;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Core.Controllers
{
    [ApiController]
    [Route("api/koala/core/editions")]
    public class EditionController : ControllerBase
    {
        private readonly EditionService _editionService;

        public EditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAndStartEdition([FromBody] CreateEditionDto createEditionDto)
        {
            var response = await _editionService.CreateEdition(User, createEditionDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{id}/name")]
        public async Task<IActionResult> UpdateEdition([FromRoute] Guid id, [FromBody] UpdateEditionNameDto updateEditionNameDto)
        {
            var response = await _editionService.UpdateEdition(User, id, updateEditionNameDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndEdition([FromRoute] Guid id)
        {
            var response = await _editionService.ExpireEdition(User, id);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEditions([FromQuery] PageQueryDto pageQueryDto, [FromQuery] EditionQueryDto editionQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEditions(User, pageQueryDto, editionQueryDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }
        
        [AllowAnonymous]
        [HttpGet("active-edition")]
        public async Task<IActionResult> GetActiveEdition()
        {
            var response = await _editionService.GetActiveEdition(User);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));
                
        }

        [Authorize]
        [HttpPost("subedition")]
        public async Task<IActionResult> CreateSubedition([FromBody] CreateSubeditionDto createSubeditionDto)
        {
            var response = await _editionService.CreateSubdition(User, createSubeditionDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("subedition/{id}/name")]
        public async Task<IActionResult> UpdateSubeditionName([FromRoute] Guid id, [FromBody] UpdateSubeditionNameDto updateSubeditionNameDto)
        {
            var response = await _editionService.UpdateSubedition(User, id, updateSubeditionNameDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("subedition")]
        public async Task<IActionResult> GetSubedition()
        {
            var response = await _editionService.GetSubedition(User);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("subeditions")]
        public async Task<IActionResult> GetSubeditions([FromRoute] PageQueryDto pageQueryDto)
        {
            var response = await _editionService.GetSubeditions(User, pageQueryDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, response.Pagination, response.Data));                
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubedition([FromRoute] Guid id)
        {
            var response = await _editionService.DeleteSubedition(User, id);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

    }
}