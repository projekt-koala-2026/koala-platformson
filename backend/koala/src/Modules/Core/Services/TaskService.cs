using System.Security.Claims;
using koala.src.Modules.Account.Services;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class TaskService
    {
        private readonly CoreDbContext _db;
        private readonly IAccountModule _accountModule;

        public TaskService(CoreDbContext db, IAccountModule accountModule)
        {
            _db = db;
            _accountModule = accountModule;
        }

        public async Task<TaskAnswerDto> CreateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, CreateTaskAnswerDto createTaskAnswerDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_item_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task item of provided id does not exist");
            }

            var newTaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == createTaskAnswerDto.Answer && tc.TaskItem.Id == task_item_id);

            if (newTaskAnswer != null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerAlreadyExsits, "Task answer for this task with this answer already exists");
            }

            newTaskAnswer = new TaskAnswer
            {
                Id = Guid.CreateVersion7(),
                Answer = createTaskAnswerDto.Answer
            };

            await _db.TaskAnswers.AddAsync(newTaskAnswer);
            await _db.SaveChangesAsync();

            return new TaskAnswerDto(
                newTaskAnswer.Id,
                newTaskAnswer.TaskItemId,
                newTaskAnswer.Answer
            );
        }

        public async Task<TaskAnswerDto> UpdateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_answer_id, UpdateTaskAnswerDto updateTaskAnswerDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            var TaskAnswerWithThisNameExists = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == updateTaskAnswerDto.Answer && tc.TaskItem.Id == task_id && tc.Id != task_answer_id);

            if (TaskAnswerWithThisNameExists != null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerAlreadyExsits, "task answer for this task with this answer already exists thus canot change this task answer to be the same ! Duplication");
            }

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Id == task_answer_id && tc.TaskItem.Id == task.Id);

            if (TaskAnswer == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task answer of provided task_answer_id and task_id does not exist");
            }

            TaskAnswer.TaskItemId = updateTaskAnswerDto.TaskItemId;
            TaskAnswer.Answer = updateTaskAnswerDto.Answer;

            await _db.SaveChangesAsync();

            return new TaskAnswerDto(
                TaskAnswer.Id,
                TaskAnswer.TaskItem.Id,
                TaskAnswer.Answer
            );
        }

        public async Task DeleteTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_answer_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItem.Id == task_id);

            if (TaskAnswer == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }

            _db.TaskAnswers.Remove(TaskAnswer);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskAnswerDto> GetTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_answer_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var TaskAnswer = await _db.TaskAnswers
                .AsNoTracking()
                .Include(tc => tc.TaskItem)
                .FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItem.Id == task_id);

            if (TaskAnswer == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }

            return new TaskAnswerDto(
                TaskAnswer.Id,
                TaskAnswer.TaskItem.Id,
                TaskAnswer.Answer
            );
        }

        public async Task<(List<TaskAnswerDto> Data, ApiPagination Pagination)> GetTaskAnswersAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var query = _db.TaskAnswers.Include(tc => tc.TaskItem).Where(tc => tc.TaskItem.Id == task_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskAnswers = entities.Select(e =>
            {
                return new TaskAnswerDto(
                    e.Id,
                    e.TaskItem.Id,
                    e.Answer
                );
            }).ToList();

            return (TaskAnswers, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskSubmissionDto> UpdateTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid team_id, SubmitTaskSubmissionDto submitTaskSubmissionDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsTeamPlayer(claimsPrincipal);
            if (!isTeamPlayer)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Only a Team Player that is a captain can use this endpoint");
            }

            var teamInfo = await _accountModule.Internal_GetTeamInfo(team_id);
            if (teamInfo.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Only a Team Player that is a captain can use this endpoint");
            }

            var task = await _db.TaskSubmissions.FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id);

            if (task == null)
            {
                task = new TaskSubmission
                {
                    TeamId = team_id,
                    TaskId = task_id,
                    Version = submitTaskSubmissionDto.Version,
                    Review1Score = null,
                    Review2Score = null,
                    Reviewer1Id = null,
                    Reviewer2Id = null,
                    AutoReviewState = null,
                    TeamAnswer = submitTaskSubmissionDto.TeamAnswer
                };
            }

            if (task.Version != submitTaskSubmissionDto.Version)
            {
                throw new CoreException(CoreErrorCodes.Conflict, "U cant update when having stale data");
            }

            task.TeamAnswer = submitTaskSubmissionDto.TeamAnswer;
            task.Version++;

            await _db.SaveChangesAsync();

            return new TaskSubmissionDto(
                task.TeamId,
                task.TaskId,
                null,
                null,
                null,
                null,
                null,
                task.TeamAnswer,
                task.Version
            );
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTaskSubmissionsAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var query = _db.TaskSubmissions.Include(t => t.TaskItem).Where(t => t.TaskItem.Id == task_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TeamId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskSubmissions = entities.Select(e =>
            {
                return new TaskSubmissionDto(
                    e.TeamId,
                    e.TaskItem.Id,
                    e.Reviewer1Id,
                    e.Reviewer2Id,
                    e.AutoReviewState,
                    e.Review1Score,
                    e.Review2Score,
                    e.TeamAnswer,
                    e.Version
                );
            }).ToList();

            return (TaskSubmissions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskSubmissionDto> UpdateTaskReviewerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, UpdateTaskSubmissionAdminDto updateTaskSubmissionAdminDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskSubmissions.FirstOrDefaultAsync(e => e.TaskId == task_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }


            task.Reviewer1Id = updateTaskSubmissionAdminDto.Reviewer1Id;
            task.Reviewer2Id = updateTaskSubmissionAdminDto.Reviewer2Id;
            task.AutoReviewState = updateTaskSubmissionAdminDto.AutoReviewState;
            task.Review1Score = updateTaskSubmissionAdminDto.Review1Score;
            task.Review2Score = updateTaskSubmissionAdminDto.Review2Score;

            await _db.SaveChangesAsync();

            return new TaskSubmissionDto(
            task.TeamId,
            task.TaskId,
            task.Reviewer1Id,
            task.Reviewer2Id,
            task.AutoReviewState,
            task.Review1Score,
            task.Review2Score,
            task.TeamAnswer,
            task.Version
            );
        }



    }

}