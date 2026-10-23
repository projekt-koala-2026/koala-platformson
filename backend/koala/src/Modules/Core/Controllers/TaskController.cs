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
        [HttpPost]
        public async Task<IActionResult> CreateTaskCorr([FromRoute] Guid id, [FromBody] CreateTaskCorrDto createTaskCorrDto)
        {
            var response = await _taskService.CreateTaskCorrAsync(User, id, createTaskCorrDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTaskCorr([FromRoute] Guid id, [FromBody] UpdateTaskCorrDto updateTaskCorrDto)
        {
            var response = await _taskService.UpdateTaskCorrAsync(User, id, updateTaskCorrDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskCorr([FromRoute] Guid id)
        {
            await _taskService.DeleteTaskCorrAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));                
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskCorr([FromRoute] Guid id)
        {
            var response = await _taskService.GetTaskCorrAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));                
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTaskCorrs([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskCorrsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskCorrDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));                
        }


    }
}