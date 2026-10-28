using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class TaskService
    {
        private readonly CoreDbContext _db;

        public TaskService(CoreDbContext db)
        {
            _db = db;
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

        public async Task<TaskAnswerDto> UpdateTaskAnswerTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid team_id, UpdateTaskAnswerTeamDto updateTaskAnswerTeamDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            var task = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.TaskId == task_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            task.TeamId = team_id; 

            if(isOrganizationAdmin)
            {
                task.Reviewer1Id = updateTaskAnswerTeamDto.Reviewer1Id;
                task.Reviewer2Id = updateTaskAnswerTeamDto.Reviewer2Id;
                task.AutoReviewState = updateTaskAnswerTeamDto.AutoReviewState;
                task.Review1State = updateTaskAnswerTeamDto.Review1State;
                task.Review2State = updateTaskAnswerTeamDto.Review2State;

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

            task.TeamAnswer = updateTaskAnswerTeamDto.TeamAnswer;
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


    }

}