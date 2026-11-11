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
        [HttpPut("{task_id}/team/{team_id}/subedition/{subedition_id}")]
        public async Task<IActionResult> UpsertTaskSubmissionTeam([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromRoute] Guid subedition_id, [FromBody] UpsertTaskSubmissionDto submitTaskSubmissionDto)
        {
            var response = await _taskService.UpdateTaskSubmissionTeamAsync(User, subedition_id, task_id, team_id, submitTaskSubmissionDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_id}/team/{team_id}/subedition/{subedition_id}")]
        public async Task<IActionResult> GetTaskSubmissionTeam([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromRoute] Guid subedition_id)
        {
            var response = await _taskService.GetTaskSubmissionTeamAsync(User, subedition_id, task_id, team_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskSubmissionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("subedition/{subedition_id}/autocheck-progress")]
        public async Task<IActionResult> GetSubeditionAutoCheckProgressAsync([FromRoute] Guid subedition_id)
        {
            var response = await _taskService.GetSubeditionAutoCheckProgressAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<AutoCheckProgressDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("subedition/{subedition_id}/autocheck")]
        public async Task<IActionResult> AutoCheckSubeditionSubmissionsAsync([FromRoute] Guid subedition_id)
        {
            await _taskService.AutoCheckSubeditionSubmissionsAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{task_id}/review-progress")]
        public async Task<IActionResult> GetTaskReviewProgressAsync([FromRoute] Guid task_id)
        {
            var response = await _taskService.GetTaskReviewProgressAsync(User, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<TaskReviewProgressDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("{task_id}/pending-reviewers")]
        public async Task<IActionResult> GetTaskPendingReviewersAsync([FromRoute] Guid task_id)
        {
            var response = await _taskService.GetTaskPendingReviewersAsync(User, task_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<PendingReviewerDto>>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpGet("submissions/{task_id}")]
        public async Task<IActionResult> GetTaskSubmissions([FromRoute] Guid task_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _taskService.GetTaskSubmissionsAsync(User, task_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<TaskSubmissionDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("review/me/submission/{task_id}/team/{team_id}/subedition/{subedition_id}")]
        public async Task<IActionResult> UpsertScoreByReviewer([FromRoute] Guid task_id, [FromRoute] Guid team_id, [FromRoute] Guid subedition_id, [FromBody] UpsertReviewDto upsertReviewDto)
        {
            var response = await _taskService.UpsertScoreByReviewerAsync(User, ClaimsHelper.GetUserGuid(User), task_id, team_id, subedition_id, upsertReviewDto);
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
        [HttpPut("{subedition_id}/assign-reviewers/task/{task_id}")]
        public async Task<IActionResult> AssignReviewers([FromRoute] Guid subedition_id, [FromRoute] Guid task_id, [FromBody] AssignReviewersDto requestDto)
        {
            await _taskService.AssignReviewersAsync(User, subedition_id, task_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("{subedition_id}/updated-assign-reviewer/task/{task_id}")]
        public async Task<IActionResult> UpdateAssignedReviewer([FromRoute] Guid subedition_id, [FromRoute] Guid task_id, [FromBody] AdminUpdateReviewersDto requestDto)
        {
            await _taskService.UpdateAssignedReviewer(User, subedition_id, task_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
    }
}
