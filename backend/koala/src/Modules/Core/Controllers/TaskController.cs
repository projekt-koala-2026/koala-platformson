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
        public async Task<IActionResult> CreateTaskAnswer([FromRoute] Guid task_id, [FromBody] TaskAnswerCreateDto createTaskAnswerDto)
        {
            var response = await _taskService.CreateTaskAnswerAsync(User, task_id, createTaskAnswerDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> UpdateTaskAnswer([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id, [FromBody] TaskAnswerUpdateDto updateTaskAnswerDto)
        {
            var response = await _taskService.UpdateTaskAnswerAsync(User, task_id, task_corr_id, updateTaskAnswerDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskAnswerDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> DeleteTaskAnswer([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id)
        {
            await _taskService.DeleteTaskAnswerAsync(User, task_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{task_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> GetTaskAnswer([FromRoute] Guid task_id, [FromRoute] Guid task_corr_id)
        {
            var response = await _taskService.GetTaskAnswerAsync(User, task_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskAnswerDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_id}/corr")]
        public async Task<IActionResult> GetTaskAnswers([FromRoute] Guid task_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskAnswersAsync(User, task_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskAnswerDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("/{task_id}/team/{team_id}")]
        public async Task<IActionResult> UpsertTaskSubmissionTeam([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromBody] TaskSubmissionUpsertDto submitTaskSubmissionDto)
        {
            var response = await _taskService.UpdateTaskSubmissionTeamAsync(User, task_id, team_id, submitTaskSubmissionDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }


        [Authorize]
        [HttpGet("/{task_id}")]
        public async Task<IActionResult> GetTaskSubmissions([FromRoute] Guid task_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskSubmissionsAsync(User, task_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPost("/to-review/me/{reviewer_id}/submission/{task_id}")]
        public async Task<IActionResult> AddReview([FromRoute] Guid reviewer_id, [FromRoute] Guid task_id, [FromBody] AddReviewDto addReviewDto)
        {
            var response = await _taskService.AddReviewAsync(User, reviewer_id, task_id, addReviewDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("/to-review/me/{reviewer_id}/submission/{task_id}")]
        public async Task<IActionResult> UpdateReview([FromRoute] Guid reviewer_id, [FromRoute] Guid task_id, [FromBody] UpdateReviewDto updateReviewDto)
        {
            var response = await _taskService.UpdateReviewAsync(User, reviewer_id, task_id, updateReviewDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("/to-review/me/{reviewer_id}")]
        public async Task<IActionResult> GetTasksToReview([FromRoute] Guid reviewer_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTasksToReviewAsync(User, reviewer_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpGet("/conflicts/me/{reviewer_id}")]
        public async Task<IActionResult> GetTasksWithConflicts([FromRoute] Guid reviewer_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTasksWithConflictsAsync(User, reviewer_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpGet("/ranking")]
        public async Task<IActionResult> GetRanking([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetRankingAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }
}