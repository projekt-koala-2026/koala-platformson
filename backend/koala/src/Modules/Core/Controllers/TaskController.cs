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
    [Route("api/koala/core/tasks")]
    public class TaskController : ControllerBase
    {
         private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        [Authorize]
        [HttpPost("subeditions/tasks/{task_id}/task_odp")]
        public async Task<IActionResult> CreateTaskCorr([FromRoute] Guid task_id, [FromBody] CreateTaskCorrDto createTaskCorrDto)
        {
            var response = await _taskService.CreateTaskCorrAsync(User, task_id, createTaskCorrDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("task_odp/{task_odp_id}")]
        public async Task<IActionResult> UpdateTaskCorr([FromRoute] Guid task_odp_id, [FromBody] UpdateTaskCorrDto updateTaskCorrDto)
        {
            var response = await _taskService.UpdateTaskCorrAsync(User, task_odp_id, updateTaskCorrDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpDelete("task_odp/{task_odp_id}")]
        public async Task<IActionResult> DeleteTaskCorr([FromRoute] Guid task_odp_id)
        {
            await _taskService.DeleteTaskCorrAsync(User, task_odp_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));                
        }

        [Authorize]
        [HttpGet("/task_odp/{task_odp_id}")]
        public async Task<IActionResult> GetTaskCorr([FromRoute] Guid task_odp_id)
        {
            var response = await _taskService.GetTaskCorrAsync(User, task_odp_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpGet("task_odp")]
        public async Task<IActionResult> GetTaskCorrs([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskCorrsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskCorrDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }


    }
}