using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Differencing;
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

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Name == createEditionDto.Name);

            if (edition != null)
            {
                throw new CoreException(CoreErrorCodes.EditionAlreadyExsits, "Edition with this name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition = new Edition
            {
                Id = Guid.CreateVersion7(),
                Name = createEditionDto.Name,
                State = EditionStates.STATE_INACTIVE,
                TeamRegisterOpen = createEditionDto.TeamRegisterOpen,
                TeamRegisterClose = createEditionDto.TeamRegisterClose,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            _db.Editions.Add(edition);
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<EditionDto> UpdateEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid id, EditionUpdateDto editionUpdateDto)
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

            bool isEditionWithNewNameAlreadyInDb = await _db.Editions.AnyAsync(e => e.Name == editionUpdateDto.Name && e.Id != id);

            if (isEditionWithNewNameAlreadyInDb == true)
            {
                throw new CoreException(CoreErrorCodes.EditionAlreadyExsits, "Edition with this name already exist thus canot update this edition name");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition of provided id does not exist");
            }

            if (edition.Version != editionUpdateDto.Version)
            {
                throw new CoreException(CoreErrorCodes.Conflict, "Editions versions do not match make sure u have the freshest data");
            }

            edition.Name = editionUpdateDto.Name;
            edition.TeamRegisterOpen = editionUpdateDto.TeamRegisterOpen;
            edition.TeamRegisterClose = editionUpdateDto.TeanRegisterClose;
            edition.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }
        public async Task<EditionDto> EditionActivateAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id)
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

            bool isAnyEditionActive = await _db.Editions.AnyAsync(e => e.State == EditionStates.STATE_ACTIVE);
            if (isAnyEditionActive)
            {
                throw new CoreException(CoreErrorCodes.ActiveEditionAlreadyExists, "There already exists another editions that is curently active canot have two editions active at once");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State == EditionStates.STATE_INACTIVE);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be activated becouse it was closed or is active already");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.State = EditionStates.STATE_ACTIVE;
            edition.UpdatedAt = timeNow;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }
        public async Task<EditionDto> EditionCloseAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id && e.State == EditionStates.STATE_ACTIVE);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be closed becouse it is not active");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.State = EditionStates.STATE_CLOSED;
            edition.UpdatedAt = timeNow;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<(List<EditionDto>, ApiPagination)> GetEditionsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, EditionQueryDto editionQueryDto)
        {
            var query = _db.Editions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(editionQueryDto.States))
            {
                var targetStates = editionQueryDto.States
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant())
                    .ToList();

                if (targetStates.Count > 0)
                {
                    query = query.Where(e => targetStates.Contains(e.State));
                }
            }

            var queryResult = await query.ToListAsync();
            var editions = queryResult
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new EditionDto
                (
                    e.Id,
                    e.Name,
                    e.State,
                    e.TeamRegisterOpen,
                    e.TeamRegisterClose,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                ))
                .ToList();

            return (editions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResult.Count));
        }

        public async Task<EditionDto> GetActiveEditionAsync(ClaimsPrincipal? claimsPrincipal)
        {
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.State == EditionStates.STATE_ACTIVE);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "There is no current active edition at the moment");
            }

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<SubEditionDto> CreateSubeditionAsync(ClaimsPrincipal? claimsPrincipal, Guid id, CreateSubeditionDto createSubeditionDto)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id);

            if (edition.State != EditionStates.STATE_CLOSED)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Subeditions can only be created for the active and inactive edition");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Name == createSubeditionDto.Name && e.EditionSubEdition.EditionId == id);

            if (subedition != null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionAlreadyExsits, "SubEdition with this name already exists canot create a new one");
            }

            DateTime timeNow = DateTime.UtcNow;


            SubEdition newSubedition = new SubEdition
            {
                Id = Guid.CreateVersion7(),
                Name = createSubeditionDto.Name,
                DateStart = createSubeditionDto.DateStart,
                DateEnd = createSubeditionDto.DateEnd,
                CreatedAt = timeNow,
                ExpiresAt = null
            };

            EditionSubEdition newEditionSubEdition = new EditionSubEdition
            {
                EditionId = id,
                SubEditionId = newSubedition.Id
            };
            await _db.SubEditions.AddAsync(newSubedition);
            await _db.EditionSubEditions.AddAsync(newEditionSubEdition);
            await _db.SaveChangesAsync();

            return new SubEditionDto(newSubedition.Id, newEditionSubEdition.EditionId, newSubedition.Name, newSubedition.DateStart, newSubedition.DateEnd, newSubedition.CreatedAt, newSubedition.ExpiresAt);
        }

        public async Task<SubEditionDto> UpdateSubeditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, UpdateSubEditionDto updateSubEditionDto)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition does not exist");
            }

            if (edition.State != EditionStates.STATE_CLOSED)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Subeditions can only be updated for the active and inactive edition");
            }

            bool isSubEditionWithNewNameAlreadyInDb = await _db.SubEditions.AnyAsync(e => e.Name == updateSubEditionDto.Name && e.Id != subedition_id && e.EditionSubEdition.EditionId == edition_id);

            if (isSubEditionWithNewNameAlreadyInDb == true)
            {
                throw new CoreException(CoreErrorCodes.SubEditionAlreadyExsits, "SubEdition with this name already exist thus canot update this subedition name to it");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition of provided id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            subedition.Name = updateSubEditionDto.Name;
            subedition.DateStart = updateSubEditionDto.DateStart;
            subedition.DateEnd = updateSubEditionDto.DateEnd;
            await _db.SaveChangesAsync();

            return new SubEditionDto(subedition.Id, subedition.EditionSubEdition.EditionId, subedition.Name, subedition.DateStart, subedition.DateEnd, subedition.CreatedAt, subedition.ExpiresAt);
        }

        public async Task<SubEditionDto> GetSubeditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
        {
            var subedition = await _db.SubEditions
                .AsNoTracking()
                .Include(e => e.EditionSubEdition)
                .FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionSubEdition.EditionId == edition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition not found");
            }

            return new SubEditionDto(subedition.Id, subedition.EditionSubEdition.EditionId, subedition.Name, subedition.DateStart, subedition.DateEnd, subedition.CreatedAt, subedition.ExpiresAt);
        }

        public async Task<(List<SubEditionDto>, ApiPagination)> GetSubeditionsAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, PageQueryDto pageQueryDto)
        {
            var query = _db.SubEditions
                .AsNoTracking()
                .Include(e => e.EditionSubEdition)
                .Where(e => e.EditionSubEdition.EditionId == edition_id);

            var totalCount = await query.CountAsync();

            var subeditions = await query
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new SubEditionDto(
                            e.Id,
                            e.EditionSubEdition.EditionId,
                            e.Name,
                            e.DateStart,
                            e.DateEnd,
                            e.CreatedAt,
                            e.ExpiresAt
                        ))
                        .ToListAsync();

            return (subeditions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task DeleteSubeditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionSubEdition.EditionId == edition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition of provided id does not exist");
            }

            _db.SubEditions.Remove(subedition);
            _db.EditionSubEditions.Remove(subedition.EditionSubEdition);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskItemDto> CreateTaskAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, CreateTaskItemDto createTaskDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionSubEdition.EditionId == edition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubeditionNotFound, "Subedition of provided id does not exist");
            }

            var newTask = await _db.Tasks.FirstOrDefaultAsync(t => t.Name == createTaskDto.Name && t.SubEditionTask.SubEditionId == subedition_id);

            if (newTask != null)
            {
                throw new CoreException(CoreErrorCodes.TaskAlreadyExsits, "Task with this name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            newTask = new TaskItem
            {
                Id = Guid.CreateVersion7(),
                Name = createTaskDto.Name,
                ContentJson = createTaskDto.ContentJson,
                UpdatedAt = timeNow
            };

            SubEditionTask newSubEditionTask = new SubEditionTask
            {
                SubEditionId = subedition_id,
                TaskId = newTask.Id
            };

            await _db.Tasks.AddAsync(newTask);
            await _db.SubEditionTasks.AddAsync(newSubEditionTask);
            await _db.SaveChangesAsync();

            return new TaskItemDto(
                newTask.Id,
                newTask.SubEditionTask.SubEditionId,
                newTask.Name,
                newTask.ContentJson,
                newTask.UpdatedAt
            );
        }

        public async Task DeleteTaskAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
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

            var task = await _db.Tasks.FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionTask.SubEditionId == subedition_id && e.SubEditionTask.SubEdition.EditionSubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskItemDto> GetTaskAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
        {
            var task = await _db.Tasks
                .AsNoTracking()
                .Include(e => e.SubEditionTask)
                .ThenInclude(e => e.SubEdition)
                .ThenInclude(e => e.EditionSubEdition)
                .FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionTask.SubEditionId == subedition_id && e.SubEditionTask.SubEdition.EditionSubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            return new TaskItemDto(
                task.Id,
                task.SubEditionTask.SubEditionId,
                task.Name,
                task.ContentJson,
                task.UpdatedAt
            );
        }

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTasksAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, PageQueryDto pageQueryDto)
        {
            var query = _db.Tasks.AsNoTracking();

            var totalCount = await query.CountAsync();
            var entities = await query
                .Include(e => e.SubEditionTask)
                .ThenInclude(e => e.SubEdition)
                .ThenInclude(e => e.EditionSubEdition)
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var tasks = entities.Where(e => e.SubEditionTask.SubEditionId == subedition_id && e.SubEditionTask.SubEdition.EditionSubEdition.EditionId == edition_id).Select(e => new TaskItemDto(
                e.Id,
                e.SubEditionTask.SubEditionId,
                e.Name,
                e.ContentJson,
                e.UpdatedAt
            )).ToList();

            return (tasks, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}