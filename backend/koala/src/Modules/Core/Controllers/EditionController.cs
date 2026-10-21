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
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/name")]
        public async Task<IActionResult> UpdateEdition([FromRoute] Guid id, [FromBody] UpdateEditionNameDto updateEditionNameDto)
        {
            var response = await _editionService.UpdateEdition(User, id, updateEditionNameDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/end")]
        public async Task<IActionResult> EndEdition([FromRoute] Guid id)
        {
            var response = await _editionService.ExpireEdition(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEditions([FromQuery] PageQueryDto pageQueryDto, [FromQuery] EditionQueryDto editionQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEditions(User, pageQueryDto, editionQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }
        
        [AllowAnonymous]
        [HttpGet("active-edition")]
        public async Task<IActionResult> GetActiveEdition()
        {
            var response = await _editionService.GetActiveEdition(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));
                
        }

        [Authorize]
        [HttpPost("{edition_id}/subeditions")]
        public async Task<IActionResult> CreateSubedition([FromRoute] Guid id, [FromBody] CreateSubeditionDto createSubeditionDto)
        {
            var response = await _editionService.CreateSubedition(User, id, createSubeditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/subeditions/{subedition_id}/name")]
        public async Task<IActionResult> UpdateSubeditionName([FromRoute] Guid id, [FromBody] UpdateSubeditionNameDto updateSubeditionNameDto)
        {
            var response = await _editionService.UpdateSubedition(User, id, updateSubeditionNameDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}")]
        public async Task<IActionResult> GetSubedition([FromRoute] Guid id, [FromRoute] Guid subeditionId)
        {
            var response = await _editionService.GetSubedition(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions")]
        public async Task<IActionResult> GetSubeditions([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetSubeditions(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }

        [Authorize]
        [HttpDelete("{edition_id}/subeditions/{subedition_id}")]
        public async Task<IActionResult> DeleteSubedition([FromRoute] Guid id)
        {
            await _editionService.DeleteSubedition(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));                
        }

        [Authorize]
        [HttpPost("{edition_id}/subeditions/{subedition_id}/tasks")]
        public async Task<IActionResult> CreateTask([FromRoute] Guid id, [FromBody] CreateTaskItemDto createTaskDto)
        {
            var response = await _editionService.CreateTask(User, id, createTaskDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpDelete("{edition_id}/subeditions/{subedition_id}/tasks/{task_id}")]
        public async Task<IActionResult> DeleteTask([FromRoute] Guid id)
        {
            await _editionService.DeleteTask(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));                
        }
        
        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}/tasks/{task_id}")]
        public async Task<IActionResult> GetTask([FromRoute] Guid id)
        {
            var response = await _editionService.GetTask(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}/tasks")]
        public async Task<IActionResult> GetTasks([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTasks(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<TaskItemDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));            
        }



    }
}