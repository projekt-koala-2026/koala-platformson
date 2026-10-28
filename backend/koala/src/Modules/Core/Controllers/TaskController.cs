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
        [HttpPost("{task_id}/corr")]
        public async Task<IActionResult> CreateTaskCorr([FromRoute] Guid task_id, [FromBody] CreateTaskCorrDto createTaskCorrDto)
        {
            var response = await _taskService.CreateTaskCorrAsync(User, task_id, createTaskCorrDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> UpdateTaskCorr([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id, [FromBody] UpdateTaskCorrDto updateTaskCorrDto)
        {
            var response = await _taskService.UpdateTaskCorrAsync(User, task_id, task_corr_id, updateTaskCorrDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> DeleteTaskCorr([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id)
        {
            await _taskService.DeleteTaskCorrAsync(User, task_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> GetTaskCorr([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id)
        {
            var response = await _taskService.GetTaskCorrAsync(User, task_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskCorrDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_id}/corr")]
        public async Task<IActionResult> GetTaskCorrs([FromRoute] Guid task_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskCorrsAsync(User, task_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskCorrDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("/{task_id}/team/{team_id}")]
        public async Task<IActionResult> UpdateTaskAnswerTeam([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromBody] UpdateTaskAnswerTeamDto updateTaskAnswerTeamDto)
        {
            var response = await _taskService.UpdateTaskAnswerTeamAsync(User, task_id, team_id, updateTaskAnswerTeamDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskAnswerDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("/{task_id}")]
        public async Task<IActionResult> GetTaskAnswers([FromRoute] Guid task_id ,[FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskAnswersAsync(User, task_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskAnswerDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        } 

    }
}