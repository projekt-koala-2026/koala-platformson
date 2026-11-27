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
        [HttpPost("etap1series/{etap1series_id}/task_item")]
        public async Task<IActionResult> CreateTaskItem([FromRoute] Guid etap1series_id, [FromBody] TaskItemCreateDto request)
        {
            var response = await _taskService.CreateTaskItemAsync(User, etap1series_id, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<TaskItemDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("etap1series/{etap1series_id}/task_item/{task_item_id}")]
        public async Task<IActionResult> UpdateTaskItem([FromRoute] Guid etap1series_id, [FromRoute] Guid task_item_id, [FromBody] TaskItemUpdateDto request)
        {
            var response = await _taskService.UpdateTaskItemAsync(User, etap1series_id, task_item_id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskItemDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("etap1series/{etap1series_id}/task_item/{task_item_id}")]
        public async Task<IActionResult> DeleteTaskItem([FromRoute] Guid etap1series_id, [FromRoute] Guid task_item_id)
        {
            await _taskService.DeleteTaskItemAsync(User, etap1series_id, task_item_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("etap1series/{etap1series_id}/task_item/{task_item_id}")]
        public async Task<IActionResult> GetTaskItem([FromRoute] Guid etap1series_id, [FromRoute] Guid task_item_id, [FromBody] TaskItemUpdateDto request)
        {
            var response = await _taskService.GetTaskItemAsync(User, etap1series_id, task_item_id);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [Authorize]
        [HttpGet("etap1series/{etap1series_id}/task_item")]
        public async Task<IActionResult> GetTaskItems([FromRoute] Guid etap1series_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var response, var pagination) = await _taskService.GetTaskItemsAsync(User, etap1series_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskItemDto>>(true, DateTime.UtcNow, null, pagination, response));
        }

        [Authorize]
        [HttpGet("active/task_items")]
        public async Task<IActionResult> GetActiveTaskItems([FromQuery] PageQueryDto pageQueryDto)
        {
            (var response, var pagination) = await _taskService.GetActiveTaskItemsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskItemDto>>(true, DateTime.UtcNow, null, pagination, response));
        }

        [Authorize]
        [HttpPost("task_item/{task_item_id}/task_part")]
        public async Task<IActionResult> CreateTaskPart([FromRoute] Guid task_part_id, [FromBody] TaskPartCreateDto request)
        {
            var response = await _taskService.CreateTaskPartAsync(User, task_part_id, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<TaskPartDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("task_item/{task_item_id}/task_part/{task_part_id}")]
        public async Task<IActionResult> UpdateTaskPart([FromRoute] Guid task_item_id, [FromRoute] Guid task_part_id, [FromBody] TaskPartUpdateDto request)
        {
            var response = await _taskService.UpdateTaskPartAsync(User, task_item_id, task_part_id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskPartDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("task_item/{task_item_id}/task_part/{task_part_id}")]
        public async Task<IActionResult> DeleteTaskPart([FromRoute] Guid task_item_id, [FromRoute] Guid task_part_id)
        {
            await _taskService.DeleteTaskPartAsync(User, task_item_id, task_part_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPost("{task_part_id}/corr")]
        public async Task<IActionResult> CreateTaskAnswer([FromRoute] Guid task_part_id, [FromBody] TaskAnswerCreateDto createTaskAnswerDto)
        {
            var response = await _taskService.CreateTaskAnswerAsync(User, task_part_id, createTaskAnswerDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{task_part_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> UpdateTaskAnswer([FromRoute] Guid task_part_id, [FromRoute] Guid task_corr_id, [FromBody] TaskAnswerUpdateDto updateTaskAnswerDto)
        {
            var response = await _taskService.UpdateTaskAnswerAsync(User, task_part_id, task_corr_id, updateTaskAnswerDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskAnswerDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{task_part_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> DeleteTaskAnswer([FromRoute] Guid task_part_id, [FromRoute] Guid task_corr_id)
        {
            await _taskService.DeleteTaskAnswerAsync(User, task_part_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{task_part_id}/corr/{task_corr_id}")]
        public async Task<IActionResult> GetTaskAnswer([FromRoute] Guid task_part_id, [FromRoute] Guid task_corr_id)
        {
            var response = await _taskService.GetTaskPartAnswerAsync(User, task_part_id, task_corr_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskAnswerDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_part_id}/corr")]
        public async Task<IActionResult> GetTaskAnswers([FromRoute] Guid task_part_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskPartAnswersAsync(User, task_part_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskAnswerDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("{task_part_id}/team/{team_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> UpsertTaskPartSubmissionTeam([FromRoute] Guid task_part_id, [FromRoute] Guid team_id, [FromRoute] Guid Etap1Series_id, [FromBody] UpsertTaskSubmissionDto submitTaskSubmissionDto)
        {
            var response = await _taskService.UpdateTaskSubmissionTeamAsync(User, Etap1Series_id, task_part_id, team_id, submitTaskSubmissionDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_id}/team/{team_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> GetTaskSubmissionTeam([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _taskService.GetTaskSubmissionTeamAsync(User, Etap1Series_id, task_id, team_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("Etap1Series/{Etap1Series_id}/autocheck-progress")]
        public async Task<IActionResult> GetEtap1SeriesAutoCheckProgressAsync([FromRoute] Guid Etap1Series_id)
        {
            var response = await _taskService.GetEtap1SeriesAutoCheckProgressAsync(User, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<AutoCheckProgressDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("Etap1Series/{Etap1Series_id}/autocheck")]
        public async Task<IActionResult> AutoCheckEtap1SeriesubmissionsAsync([FromRoute] Guid Etap1Series_id)
        {
            await _taskService.AutoCheckEtap1SeriesubmissionsAsync(User, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("task_item/{task_item_id}/review-progress")]
        public async Task<IActionResult> GetTaskReviewProgressAsync([FromRoute] Guid task_item_id)
        {
            var response = await _taskService.GetTaskItemReviewProgressAsync(User, task_item_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskReviewProgressDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("task_item/{task_item_id}/pending-reviewers")]
        public async Task<IActionResult> GetTaskPendingReviewersAsync([FromRoute] Guid task_item_id)
        {
            var response = await _taskService.GetTaskItemPendingReviewersAsync(User, task_item_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<PendingReviewerDto>>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("submissions/{task_item_id}")]
        public async Task<IActionResult> GetTaskSubmissions([FromRoute] Guid task_item_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskItemSubmissionsAsync(User, task_item_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("review/me/submission/{task_part_id}/team/{team_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> UpsertScoreByReviewer([FromRoute] Guid task_part_id, [FromRoute] Guid team_id, [FromRoute] Guid Etap1Series_id, [FromBody] UpsertReviewDto upsertReviewDto)
        {
            var response = await _taskService.UpsertScoreByReviewerAsync(User, ClaimsHelper.GetUserGuid(User), task_part_id, team_id, Etap1Series_id, upsertReviewDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("review/me")]
        public async Task<IActionResult> GetTasksToReview([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTasksToReviewAsync(User, ClaimsHelper.GetUserGuid(User), pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpGet("review-conflicts/me/")]
        public async Task<IActionResult> GetTasksWithConflicts([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTasksWithConflictsAsync(User, ClaimsHelper.GetUserGuid(User), pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("{Etap1Series_id}/assign-reviewers/task_item/{task_item_id}")]
        public async Task<IActionResult> AssignReviewers([FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_item_id, [FromBody] AssignReviewersDto requestDto)
        {
            await _taskService.AssignReviewersAsync(User, Etap1Series_id, task_item_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("{Etap1Series_id}/updated-assign-reviewer/task_item/{task_item_id}")]
        public async Task<IActionResult> UpdateAssignedReviewer([FromRoute] Guid Etap1Series_id, [FromRoute] Guid task_item_id, [FromBody] AdminUpdateReviewersDto requestDto)
        {
            await _taskService.UpdateAssignedReviewer(User, Etap1Series_id, task_item_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
    }
}
