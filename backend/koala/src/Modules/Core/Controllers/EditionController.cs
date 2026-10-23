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
            var response = await _editionService.CreateEditionAsync(User, createEditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/name")]
        public async Task<IActionResult> UpdateEdition([FromRoute] Guid edition_id, [FromBody] UpdateEditionNameDto updateEditionNameDto)
        {
            var response = await _editionService.UpdateEditionAsync(User, edition_id, updateEditionNameDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/end")]
        public async Task<IActionResult> EndEdition([FromRoute] Guid edition_id)
        {
            var response = await _editionService.ExpireEditionAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEditions([FromQuery] PageQueryDto pageQueryDto, [FromQuery] EditionQueryDto editionQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEditionsAsync(User, pageQueryDto, editionQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }

        [AllowAnonymous]
        [HttpGet("active-edition")]
        public async Task<IActionResult> GetActiveEdition()
        {
            var response = await _editionService.GetActiveEditionAsync(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
                
        }

        [Authorize]
        [HttpPost("{edition_id}/subeditions")]
        public async Task<IActionResult> CreateSubedition([FromRoute] Guid edition_id, [FromBody] CreateSubeditionDto createSubeditionDto)
        {
            var response = await _editionService.CreateSubeditionAsync(User, edition_id, createSubeditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{edition_id}/subeditions/{subedition_id}/name")]
        public async Task<IActionResult> UpdateSubedition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromBody] UpdateSubEditionNameDto updateSubEditionNameDto)
        {
            var response = await _editionService.UpdateSubeditionAsync(User, edition_id, subedition_id, updateSubEditionNameDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("subeditions/{subedition_id}")]
        public async Task<IActionResult> GetSubedition([FromRoute] Guid subedition_id)
        {
            var response = await _editionService.GetSubeditionAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions")]
        public async Task<IActionResult> GetSubeditions([FromRoute] Guid edition_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetSubeditionsAsync(User, edition_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }

        [Authorize]
        [HttpDelete("subeditions/{subedition_id}")]
        public async Task<IActionResult> DeleteSubedition([FromRoute] Guid subedition_id)
        {
            await _editionService.DeleteSubeditionAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));                
        }

        [Authorize]
        [HttpPost("subeditions/{subedition_id}/tasks")]
        public async Task<IActionResult> CreateTask([FromRoute] Guid subedition_id, [FromBody] CreateTaskItemDto createTaskDto)
        {
            var response = await _editionService.CreateTaskAsync(User, subedition_id, createTaskDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpDelete("subeditions/tasks/{task_id}")]
        public async Task<IActionResult> DeleteTask([FromRoute] Guid task_id)
        {
            await _editionService.DeleteTaskAsync(User, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));                
        }
        
        [AllowAnonymous]
        [HttpGet("subeditions/tasks/{task_id}")]
        public async Task<IActionResult> GetTask([FromRoute] Guid task_id)
        {
            var response = await _editionService.GetTaskAsync(User, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [AllowAnonymous]
        [HttpGet("subeditions/tasks")]
        public async Task<IActionResult> GetTasks([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTasksAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskItemDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));            
        }        

    }
}