using koala.src.Modules.Account.Entities;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

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
        [HttpPost("{edition_id}/Etap1Series")]
        public async Task<IActionResult> CreateEtap1Series([FromRoute] Guid edition_id, [FromBody] Etap1SeriesCreateDto createEtap1SeriesDto)
        {
            var response = await _editionService.CreateEtap1SeriesAsync(User, edition_id, createEtap1SeriesDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> UpdateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromBody] Etap1SeriesUpdateDto updateEtap1SeriesDto)
        {
            var response = await _editionService.UpdateEtap1SeriesAsync(User, edition_id, Etap1Series_id, updateEtap1SeriesDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> GetEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.GetEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/Etap1Series")]
        public async Task<IActionResult> GetEtap1Series([FromRoute] Guid edition_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEtap1SeriesAsync(User, edition_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("{edition_id}/activate_Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> ActivateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.ActivateEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/close_Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> DeactivateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.CloseEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> DeleteEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            await _editionService.DeleteEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPost("{edition_id}/Etap1Series/{Etap1Series_id}/tasks")]
        public async Task<IActionResult> CreateTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromBody] TaskItemCreateDto createTaskDto)
        {
            var response = await _editionService.CreateTaskItemAsync(User, edition_id, Etap1Series_id, createTaskDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}")]
        public async Task<IActionResult> UpdateTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id, [FromBody] TaskItemUpdateDto updateTaskDto)
        {
            var response = await _editionService.UpdateTaskItemAsync(User, edition_id, Etap1Series_id, task_id, updateTaskDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskItemDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}")]
        public async Task<IActionResult> DeleteTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id)
        {
            await _editionService.DeleteTaskItemAsync(User, edition_id, Etap1Series_id, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}")]
        public async Task<IActionResult> GetTaskItem([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id)
        {
            var response = await _editionService.GetTaskItemAsync(User, edition_id, Etap1Series_id, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskItemDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}/image")]
        public async Task<IActionResult> DeleteTaskItemImage([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id)
        {
            await _editionService.DeleteTaskItemImageAsync(User, edition_id, Etap1Series_id, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}/image")]
        public async Task<IActionResult> GetTaskItemImage([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id)
        {
            var physicalPath = await _editionService.GetTaskItemImageAsync(User, edition_id, Etap1Series_id, task_id);
            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(physicalPath, out var contentType);
            return PhysicalFile(physicalPath, contentType ?? "application/octet-stream");
        }

        [Authorize]
        [HttpPut("{edition_id}/Etap1Series/{Etap1Series_id}/tasks/{task_id}/image")]
        public async Task<IActionResult> UpsertTaskItemImage([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_id, [FromForm] TaskItemImageUpsertDto request)
        {
            var physicalPath = await _editionService.UpsertTaskItemImageAsync(User, edition_id, Etap1Series_id, task_id, request);
            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(physicalPath, out var contentType);
            return PhysicalFile(physicalPath, contentType ?? "application/octet-stream");
        }

        [Authorize]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}/tasks")]
        public async Task<IActionResult> GetTaskItems([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTaskItemsAsync(User, edition_id, Etap1Series_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskItemDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}/task/active/{team_id}")]
        public async Task<IActionResult> GetTasksActive([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromRoute] Guid team_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetTasksActiveAsync(User, edition_id, Etap1Series_id, team_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }
}
