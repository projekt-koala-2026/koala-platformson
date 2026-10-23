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

        public async Task<TaskCorrDto> CreateTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, CreateTaskCorrDto createTaskCorrDto)
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

            var task = await _db.Tasks.FirstOrDefaultAsync(e => e.Id == createTaskCorrDto.TaskItemId);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task odp of provided id does not exist");
            }

            TaskCorr newTaskCorr = new TaskCorr
            {
                Id = Guid.CreateVersion7(),
                TaskId = task.Id,
                CorrectAnswer = createTaskCorrDto.CorrectAnswer
            };

            await _db.TaskCorrs.AddAsync(newTaskCorr);
            await _db.SaveChangesAsync();

            return new TaskCorrDto(
                newTaskCorr.Id,
                newTaskCorr.TaskId,
                newTaskCorr.CorrectAnswer
            );
        }

        public async Task<TaskCorrDto> UpdateTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateTaskCorrDto updateTaskCorrDto)
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

            var task = await _db.Tasks.FirstOrDefaultAsync(e => e.Id == id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            TaskCorr newTaskCorr = new TaskCorr
            {
                Id = id,
                TaskId = updateTaskCorrDto.TaskItemId,
                CorrectAnswer = updateTaskCorrDto.CorrectAnswer
            };

            await _db.SaveChangesAsync();

            return new TaskCorrDto(
                newTaskCorr.Id,
                newTaskCorr.TaskId,
                newTaskCorr.CorrectAnswer
            );
        }

        public async Task DeleteTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var TaskCorr = await _db.TaskCorrs.FirstOrDefaultAsync(e => e.Id == id);

            if (TaskCorr == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task Odp of provided id does not exist");
            }

            _db.TaskCorrs.Remove(TaskCorr);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskCorrDto> GetTaskCorrAsync(ClaimsPrincipal? claimsPrincipal, Guid id) 
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
                .Include(e => e.TaskItemTaskCorr)
                    .ThenInclude(link => link.TaskItem)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (taskCorr == null)
            {
                throw new CoreException(CoreErrorCodes.TaskCorrNotFound, "Task Odp of provided id does not exist");
            }

            return new TaskCorrDto(
                taskCorr.Id,
                taskCorr.TaskId,
                taskCorr.CorrectAnswer
            );
        }

        public async Task<(List<TaskCorrDto> Data, ApiPagination Pagination)> GetTaskCorrsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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

            var query = _db.TaskCorrs.AsNoTracking();

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
                    e.TaskId,
                    e.CorrectAnswer
                );
            }).ToList();

            return (TaskCorrs, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

    }

}