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

        public async Task<TaskCorrDto> CreateTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, CreateTaskCorrDto createTaskCorrDto)
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

            var task = await _db.Tasks.FirstOrDefaultAsync(e => e.Id == task_item_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task odp of provided id does not exist");
            }

            var newTaskCorr = await _db.TaskCorrs.FirstOrDefaultAsync(tc => tc.CorrectAnswer == createTaskCorrDto.CorrectAnswer && tc.TaskItemTaskCorr.TaskItemId == task_item_id);

            if (newTaskCorr != null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrAlreadyExsits, "task corr for this task with this answer already exists");
            }

            newTaskCorr = new TaskCorr
            {
                Id = Guid.CreateVersion7(),
                CorrectAnswer = createTaskCorrDto.CorrectAnswer
            };

            TaskItemTaskCorr newTaskItemTaskCorr = new TaskItemTaskCorr
            {
                TaskItemId = task_item_id,
                TaskCorrId = newTaskCorr.Id
            };

            await _db.TaskCorrs.AddAsync(newTaskCorr);
            await _db.TaskItemTaskCorrs.AddAsync(newTaskItemTaskCorr);
            await _db.SaveChangesAsync();

            return new TaskCorrDto(
                newTaskCorr.Id,
                newTaskCorr.TaskItemTaskCorr.TaskItemId,
                newTaskCorr.CorrectAnswer
            );
        }

        public async Task<TaskCorrDto> UpdateTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_corr_id, UpdateTaskCorrDto updateTaskCorrDto)
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

            var task = await _db.Tasks.FirstOrDefaultAsync(e => e.Id == task_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            var taskCorrWithThisNameExists = await _db.TaskCorrs.FirstOrDefaultAsync(tc => tc.CorrectAnswer == updateTaskCorrDto.CorrectAnswer && tc.TaskItemTaskCorr.TaskItemId == task_id && tc.Id != task_corr_id);

            if (taskCorrWithThisNameExists != null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrAlreadyExsits, "task corr for this task with this answer already exists thus canot change this task answer to be the same ! Duplication");
            }

            var taskCorr = await _db.TaskCorrs.FirstOrDefaultAsync(tc => tc.Id == task_corr_id && tc.TaskItemTaskCorr.TaskItemId == task.Id);

            if (taskCorr == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task corr of provided task_core_id and task_id does not exist");
            }

            taskCorr.TaskItemTaskCorr.TaskItemId = updateTaskCorrDto.TaskItemId;
            taskCorr.CorrectAnswer = updateTaskCorrDto.CorrectAnswer;

            await _db.SaveChangesAsync();

            return new TaskCorrDto(
                taskCorr.Id,
                taskCorr.TaskItemTaskCorr.TaskItemId,
                taskCorr.CorrectAnswer
            );
        }

        public async Task DeleteTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_corr_id)
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

            var TaskCorr = await _db.TaskCorrs.FirstOrDefaultAsync(e => e.Id == task_corr_id && e.TaskItemTaskCorr.TaskItemId == task_id);

            if (TaskCorr == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task Odp of provided id does not exist");
            }

            _db.TaskCorrs.Remove(TaskCorr);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskCorrDto> GetTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_corr_id)
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

            var taskCorr = await _db.TaskCorrs
                .AsNoTracking()
                .Include(tc => tc.TaskItemTaskCorr)
                .FirstOrDefaultAsync(e => e.Id == task_corr_id && e.TaskItemTaskCorr.TaskItemId == task_id);

            if (taskCorr == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task Odp of provided id does not exist");
            }

            return new TaskCorrDto(
                taskCorr.Id,
                taskCorr.TaskItemTaskCorr.TaskItemId,
                taskCorr.CorrectAnswer
            );
        }

        public async Task<(List<TaskCorrDto> Data, ApiPagination Pagination)> GetTaskCorrsAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, PageQueryDto pageQueryDto)
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

            var query = _db.TaskCorrs.Include(tc => tc.TaskItemTaskCorr).Where(tc => tc.TaskItemTaskCorr.TaskItemId == task_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskCorrs = entities.Select(e =>
            {
                return new TaskCorrDto(
                    e.Id,
                    e.TaskItemTaskCorr.TaskItemId,
                    e.CorrectAnswer
                );
            }).ToList();

            return (TaskCorrs, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskAnswerDto> UpdateTaskAnswerTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid team_id, SubmitTaskAnswerDto submitTaskAnswerDto)
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

            var task = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id);

            if (task == null)
            {
                task = new TaskAnswer
                {
                    TeamId = team_id,
                    TaskId = task_id,
                    Version = submitTaskAnswerDto.Version,
                    Review1State = null,
                    Review2State = null,
                    Reviewer1Id = null,
                    Reviewer2Id = null,
                    AutoReviewState = null,
                    TeamAnswer = submitTaskAnswerDto.TeamAnswer
                };
            }

            if (task.Version != submitTaskAnswerDto.Version)
            {
                throw new CoreException(CoreErrorCodes.Conflict, "U cant update when having stale data");
            }

            task.TeamAnswer = submitTaskAnswerDto.TeamAnswer;
            task.Version++;

            await _db.SaveChangesAsync();

            return new TaskAnswerDto(
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

        public async Task<(List<TaskAnswerDto>, ApiPagination)> GetTaskAnswersAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, PageQueryDto pageQueryDto)
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

            var query = _db.TaskAnswers.Include(t => t.TaskItem).Where(t => t.TaskItem.Id == task_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TeamId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskAnswers = entities.Select(e =>
            {
                return new TaskAnswerDto(
                    e.TeamId,
                    e.TaskItem.Id,
                    e.Reviewer1Id,
                    e.Reviewer2Id,
                    e.AutoReviewState,
                    e.Review1State,
                    e.Review2State,
                    e.TeamAnswer,
                    e.Version
                );
            }).ToList();

            return (TaskAnswers, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskAnswerDto> UpdateTaskAnswerTeamAdminAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, UpdateTaskAnswerAdminDto updateTaskAnswerAdminDto)
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

            var task = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.TaskId == task_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }


            task.Reviewer1Id = updateTaskAnswerAdminDto.Reviewer1Id;
            task.Reviewer2Id = updateTaskAnswerAdminDto.Reviewer2Id;
            task.AutoReviewState = updateTaskAnswerAdminDto.AutoReviewState;
            task.Review1State = updateTaskAnswerAdminDto.Review1State;
            task.Review2State = updateTaskAnswerAdminDto.Review2State;

            await _db.SaveChangesAsync();

            return new TaskAnswerDto(
            task.TeamId,
            task.TaskId,
            task.Reviewer1Id,
            task.Reviewer2Id,
            task.AutoReviewState,
            task.Review1State,
            task.Review2State,
            task.TeamAnswer,
            task.Version
            );
        }



    }

}