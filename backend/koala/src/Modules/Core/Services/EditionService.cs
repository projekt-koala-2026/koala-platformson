using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class EditionService
    {
        private readonly CoreDbContext _db;

        public EditionService(CoreDbContext db)
        {
            _db = db;
        }

        public async Task<EditionDto> CreateEditionAsync(ClaimsPrincipal? claimsPrincipal, CreateEditionDto createEditionDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not logged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permission to perform this operation on resource");
            }

            bool isThereAnActiveEdition = _db.Editions.Where(e=> e.ExpiresAt == null).AsNoTracking().Any(); 

            if(isThereAnActiveEdition)
            {
                throw new CoreException(CoreErrorCodes.ActiveEditionAlreadyExists,"There is already an active edition, cannot create a new one without ending last one first");
            }

            DateTime timeNow = DateTime.UtcNow;

            Edition edition = new Edition
            {
                Id = Guid.CreateVersion7(),
                Name = createEditionDto.Name,
                CreatedAt = timeNow,
                ExpiresAt = null
            };

            _db.Editions.Add(edition);
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<EditionDto> UpdateEdition(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateEditionNameDto updateEditionNameDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not logged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permission to perform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e=> e.Id == id); 

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"Edition of provided id does not exist");
            }

            edition.Name = updateEditionNameDto.Name;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<EditionDto> ExpireEdition(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permision to peform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e=> e.Id == id); 

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"Edition of provided id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.ExpiresAt = timeNow;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.CreatedAt, edition.ExpiresAt);
        }

        public async Task<(List<EditionDto>, ApiPagination)> GetEditions(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, EditionQueryDto editionQueryDto)
        {
            var query = _db.Editions.AsNoTracking().AsQueryable();

            if(editionQueryDto.ShowActive != null)
            {
                if(editionQueryDto.ShowActive.Value)
                {
                    query = query.Where(e => e.ExpiresAt == null);
                }
                else
                {
                    query = query.Where(e => e.ExpiresAt != null);
                }
            }

            var queryResult = await query.ToListAsync();
            var editions = queryResult
                .Skip(pageQueryDto.PageSize*pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new EditionDto
                (
                    e.Id,
                    e.Name,
                    e.CreatedAt,
                    e.ExpiresAt
                ))
                .ToList();

            return (editions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task<EditionDto> GetActiveEdition(ClaimsPrincipal? claimsPrincipal)
        {
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.ExpiresAt == null);

            if(edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound,"There is no current active edition at the moment");
            }

            return new EditionDto(edition.Id,edition.Name,edition.CreatedAt,edition.ExpiresAt);
        }

        public async Task<SubEditionDto> CreateSubedition(ClaimsPrincipal? claimsPrincipal, Guid editionId, CreateSubeditionDto createSubeditionDto)
        {
            
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permision to peform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == editionId);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition does not exist");
            }

            if (edition.ExpiresAt != null)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Subeditions can only be created for the current active edition");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e=> e.Name == createSubeditionDto.Name); 

            if(subedition != null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound,"Subedition of provided name does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;


            SubEdition newSubedition = new SubEdition
            {
                Id = Guid.CreateVersion7(),
                EditionId = edition.Id,
                Name = createSubeditionDto.Name,
                DateStart = createSubeditionDto.DateStart,
                DateEnd = createSubeditionDto.DateEnd,
                CreatedAt = timeNow,
                ExpiresAt = null
            };

            await _db.SubEditions.AddAsync(newSubedition);
            await _db.SaveChangesAsync();

            return new SubEditionDto(newSubedition.Id, newSubedition.EditionId, newSubedition.Name, newSubedition.DateStart, newSubedition.DateEnd, newSubedition.CreatedAt, newSubedition.ExpiresAt);
        }

        public async Task<SubEditionDto> UpdateSubedition(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateSubeditionNameDto updateSubeditionNameDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permision to peform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Name == updateSubeditionNameDto.Name);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition does not exist");
            }

            if (edition.ExpiresAt != null)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Subeditions can only be created for the current active edition");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e=> e.Name == updateSubeditionNameDto.Name); 

            if(subedition != null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound,"Subedition of provided name does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            subedition.Name = updateSubeditionNameDto.Name;
            subedition.DateStart = updateSubeditionNameDto.DateStart;
            subedition.DateEnd = updateSubeditionNameDto.DateEnd;
            await _db.SaveChangesAsync();

            return new SubEditionDto(subedition.Id, subedition.EditionId, subedition.Name, subedition.DateStart, subedition.DateEnd, subedition.CreatedAt, subedition.ExpiresAt);
        }

        public async Task<SubEditionDto> GetSubedition(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            var subedition = await _db.SubEditions
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition not found");
            }

            return new SubEditionDto(subedition.Id, subedition.EditionId, subedition.Name, subedition.DateStart, subedition.DateEnd, subedition.CreatedAt, subedition.ExpiresAt);
        }

        public async Task<(List<SubEditionDto>, ApiPagination)> GetSubeditions(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            var queryResult = await _db.SubEditions.AsNoTracking().ToListAsync();
            var subeditions = queryResult
                .Skip(pageQueryDto.PageSize*pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new SubEditionDto
                (
                    e.Id,
                    e.EditionId,
                    e.Name,
                    e.DateStart,
                    e.DateEnd,
                    e.CreatedAt,
                    e.ExpiresAt
                ))
                .ToList();

            return (subeditions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task DeleteSubedition(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden,"User does not have permision to peform this operation on resource");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e=> e.Id == id); 

            if(subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound,"Subedition of provided id does not exist");
            }

            _db.SubEditions.Remove(subedition);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskItemDto> CreateTask(ClaimsPrincipal? claimsPrincipal, Guid subeditionId, CreateTaskItemDto createTaskDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subeditionId); 

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition of provided id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            TaskItem newTask = new TaskItem
            {
                Id = Guid.CreateVersion7(),
                EditionId = subedition.EditionId,
                SubEditionId = subeditionId,
                Name = createTaskDto.Name,
                ContentJson = createTaskDto.ContentJson,
                CreatedAt = timeNow,
                ExpiredAt = createTaskDto.ExpiredAt
            };

            await _db.Tasks.AddAsync(newTask);
            await _db.SaveChangesAsync();

            return new TaskItemDto(
                newTask.Id,
                newTask.EditionId,
                newTask.SubEditionId,
                newTask.Name,
                newTask.ContentJson,
                newTask.CreatedAt,
                newTask.ExpiredAt
            );
        }

        public async Task DeleteTask(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

        }

        public async Task<TaskItemDto> GetTask(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            var task = await _db.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            return new TaskItemDto(
                task.Id,
                task.EditionId,
                task.SubEditionId,
                task.Name,
                task.ContentJson,
                task.CreatedAt,
                task.ExpiredAt
            );
        }

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTasks(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            var query = _db.Tasks.AsNoTracking();

            var totalCount = await query.CountAsync();
            var entities = await query
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var tasks = entities.Select(e => new TaskItemDto(
                e.Id,
                e.EditionId,
                e.SubEditionId,
                e.Name,
                e.ContentJson,
                e.CreatedAt,
                e.ExpiredAt
            )).ToList();

            return (tasks, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskOdpDto> CreateTaskOdp(ClaimsPrincipal? claimsPrincipal, Guid id, CreateTaskOdpDto createTaskOdpDto)
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

            var task = await _db.Tasks
                .Include(e => e.SubEditions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskOdpNotFound, "Task odp of provided id does not exist");
            }

            TaskOdp newTaskOdp = new TaskOdp
            {
                Id = Guid.CreateVersion7(),
                TaskId = task.Id,
                CorrectAnswer = createTaskOdpDto.CorrectAnswer
            };

            await _db.TaskOdps.AddAsync(newTaskOdp);
            await _db.SaveChangesAsync();

            return new TaskOdpDto(
                newTaskOdp.Id,
                task.SubEditions.First().EditionId,
                task.SubEditionId,
                newTaskOdp.TaskId,
                newTaskOdp.CorrectAnswer
            );
        }

        public async Task<TaskOdpDto> UpdateTaskOdp(ClaimsPrincipal? claimsPrincipal, Guid id, UpdateTaskOdpDto updateTaskOdpDto)
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

            var task = await _db.Tasks
                .Include(e => e.SubEditions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            TaskOdp newTaskOdp = new TaskOdp
            {
                TaskId = task.Id,
                CorrectAnswer = updateTaskOdpDto.CorrectAnswer
            };

            await _db.SaveChangesAsync();

            return new TaskOdpDto(
                newTaskOdp.Id,
                task.SubEditions.First().EditionId,
                task.SubEditionId,
                newTaskOdp.TaskId,
                newTaskOdp.CorrectAnswer
            );
        }

        public async Task DeleteTaskOdp(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var taskOdp = await _db.TaskOdps.FirstOrDefaultAsync(e => e.Id == id);

            if (taskOdp == null)
            {
                throw new CoreException(CoreErrorCodes.TaskOdpNotFound, "Task Odp of provided id does not exist");
            }

            _db.TaskOdps.Remove(taskOdp);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskOdpDto> GetTaskOdp(ClaimsPrincipal? claimsPrincipal, Guid id) 
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
            
            var taskOdp = await _db.TaskOdps
                .Include(e => e.TaskItems)
                    .ThenInclude(t => t.SubEditions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (taskOdp == null)
            {
                throw new CoreException(CoreErrorCodes.TaskOdpNotFound, "Task Odp of provided id does not exist");
            }

            var subEdition = taskOdp.TaskItems?.SubEditions?.FirstOrDefault();

            return new TaskOdpDto(
                taskOdp.Id,
                subEdition.EditionId,
                subEdition.Id,
                taskOdp.TaskId,
                taskOdp.CorrectAnswer
            );
        }

        public async Task<(List<TaskOdpDto> Data, ApiPagination Pagination)> GetTaskOdps(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
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

            var query = _db.TaskOdps
                .AsNoTracking()
                .Include(e => e.TaskItems)
                    .ThenInclude(t => t.SubEditions);

            var totalCount = await query.CountAsync();
            
            var entities = await query
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var taskOdps = entities.Select(e =>
            {
                var subEdition = e.TaskItems?.SubEditions?.FirstOrDefault();

                return new TaskOdpDto(
                    e.Id,
                    subEdition.EditionId,
                    subEdition.Id,
                    e.TaskId,
                    e.CorrectAnswer
                );
            }).ToList();

            return (taskOdps, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}