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
        public async Task<IActionResult> CreateAndStartEdition([FromBody] EditionCreateDto createEditionDto)
        {
            var response = await _editionService.CreateEditionAsync(User, createEditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}")]
        public async Task<IActionResult> UpdateEdition([FromRoute] Guid edition_id, [FromBody] EditionUpdateDto editionUpdateDto)
        {
            var response = await _editionService.UpdateEditionAsync(User, edition_id, editionUpdateDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/activate")]
        public async Task<IActionResult> EditionActivate([FromRoute] Guid edition_id)
        {
            var response = await _editionService.EditionActivateAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/close")]
        public async Task<IActionResult> EditionClose([FromRoute] Guid edition_id)
        {
            var response = await _editionService.EditionCloseAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
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
        public async Task<IActionResult> CreateSubEdition([FromRoute] Guid edition_id, [FromBody] SubEditionCreateDto createSubEditionDto)
        {
            var response = await _editionService.CreateSubEditionAsync(User, edition_id, createSubEditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/subeditions/{subedition_id}")]
        public async Task<IActionResult> UpdateSubEdition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromBody] SubEditionUpdateDto updateSubEditionDto)
        {
            var response = await _editionService.UpdateSubEditionAsync(User, edition_id, subedition_id, updateSubEditionDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}")]
        public async Task<IActionResult> GetSubEdition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id)
        {
            var response = await _editionService.GetSubEditionAsync(User, edition_id, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions")]
        public async Task<IActionResult> GetSubEditions([FromRoute] Guid edition_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetSubEditionsAsync(User, edition_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("{edition_id}/activate_subedition/{subedition_id}")]
        public async Task<IActionResult> ActivateSubEdition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id)
        {
            var response = await _editionService.ActivateSubEditionAsync(User, edition_id, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/close_subedition/{subedition_id}")]
        public async Task<IActionResult> DeactivateSubEdition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id)
        {
            var response = await _editionService.CloseSubEditionAsync(User, edition_id, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/subeditions/{subedition_id}")]
        public async Task<IActionResult> DeleteSubEdition([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id)
        {
            await _editionService.DeleteSubEditionAsync(User, edition_id, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPost("{edition_id}/subeditions/{subedition_id}/tasks")]
        public async Task<IActionResult> CreateTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromBody] TaskItemCreateDto createTaskDto)
        {
            var response = await _editionService.CreateTaskItemAsync(User, edition_id, subedition_id, createTaskDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/subeditions/{subedition_id}/tasks/{task_id}")]
        public async Task<IActionResult> UpdateTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromRoute] Guid task_id, [FromBody] TaskItemUpdateDto updateTaskDto)
        {
            var response = await _editionService.UpdateTaskItemAsync(User, edition_id, subedition_id, task_id, updateTaskDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskItemDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/subeditions/{subedition_id}/tasks/{task_id}")]
        public async Task<IActionResult> DeleteTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromRoute] Guid task_id)
        {
            await _editionService.DeleteTaskItemAsync(User, edition_id, subedition_id, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}/tasks/{task_id}")]
        public async Task<IActionResult> GetTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromRoute] Guid task_id)
        {
            var response = await _editionService.GetTaskItemAsync(User, edition_id, subedition_id, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}/tasks")]
        public async Task<IActionResult> GetTaskItems([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTaskItemsAsync(User, edition_id, subedition_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskItemDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/subeditions/{subedition_id}/task/active/{team_id}")]
        public async Task<IActionResult> GetTasksActive([FromRoute] Guid edition_id, [FromRoute] Guid subedition_id, [FromRoute] Guid team_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTasksActiveAsync(User, edition_id, subedition_id, team_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }
}
