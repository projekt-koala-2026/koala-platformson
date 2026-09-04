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
        public async Task<IActionResult> GetEditions([FromRoute] PageQueryDto pageQueryDto)
        {
            var response = await _editionService.GetEditions(User, pageQueryDto);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, response.Pagination, response.Data));                
        }
        
        [AllowAnonymous]
        [HttpGet("active-edition")]
        public async Task<IActionResult> GetActiveEdition()
        {
            var response = await _editionService.GetActiveEdition(User);
            return StatusCode(200, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

    }
}