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

        public async Task<EditionDto> CreateEditionAsync(ClaimsPrincipal? claimsPrincipal, EditionCreateDto createEditionDto)
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

            if (editionQueryDto.States.Count != 0)
            {
                foreach (var state in editionQueryDto.States)
                {
                    if (!string.IsNullOrWhiteSpace(state))
                    {
                        if (state != EditionStates.STATE_ACTIVE || state != EditionStates.STATE_INACTIVE || state != EditionStates.STATE_CLOSED)
                        {
                            throw new CoreException(CoreErrorCodes.BadRequest, "The states are not valid states");
                        }
                    }
                }

            }

            query = query.Where(e => editionQueryDto.States.Contains(e.State));

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

        public async Task<SubEditionDto> CreateSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid id, SubEditionCreateDto createSubEditionDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Name == createSubEditionDto.Name && e.EditionId == id);

            if (subedition != null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionAlreadyExsits, "SubEdition with this name already exists canot create a new one");
            }

            DateTime timeNow = DateTime.UtcNow;


            SubEdition newSubEdition = new SubEdition
            {
                Id = Guid.CreateVersion7(),
                Name = createSubEditionDto.Name,
                AnswerOpen = createSubEditionDto.AnswerOpen,
                AnswerClose = createSubEditionDto.AnswerClose,
                State = SubEditionStates.STATE_INACTIVE,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            await _db.SubEditions.AddAsync(newSubEdition);
            await _db.SaveChangesAsync();

            return new SubEditionDto
            (
                newSubEdition.Id,
                newSubEdition.EditionId,
                newSubEdition.Name,
                newSubEdition.AnswerOpen,
                newSubEdition.AnswerClose,
                newSubEdition.State,
                newSubEdition.CreatedAt,
                newSubEdition.UpdatedAt,
                newSubEdition.Version
            );
        }

        public async Task<SubEditionDto> UpdateSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, SubEditionUpdateDto updateSubEditionDto)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State != EditionStates.STATE_CLOSED);

            if (edition == null)
            {
                throw new CoreException(CoreErrorCodes.EditionNotFound, "Edition does not exist or is closed");
            }

            bool isSubEditionWithNewNameAlreadyInDb = await _db.SubEditions.AnyAsync(e => e.Name == updateSubEditionDto.Name && e.Id != subedition_id && e.EditionId == edition_id);

            if (isSubEditionWithNewNameAlreadyInDb == true)
            {
                throw new CoreException(CoreErrorCodes.SubEditionAlreadyExsits, "SubEdition with this name already exist thus canot update this subedition name to it");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.State != SubEditionStates.STATE_CLOSED);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is closed");
            }

            if (subedition.Version != updateSubEditionDto.Version)
            {
                throw new CoreException(CoreErrorCodes.Conflict, "SubEditions versions do not match make sure u have the freshest data");
            }

            DateTime timeNow = DateTime.UtcNow;

            subedition.Name = updateSubEditionDto.Name;
            subedition.AnswerOpen = updateSubEditionDto.AnswerOpen;
            subedition.AnswerClose = updateSubEditionDto.AnswerClose;
            subedition.UpdatedAt = timeNow;
            subedition.Version++;
            await _db.SaveChangesAsync();

            return new SubEditionDto
            (
                subedition.Id,
                subedition.EditionId,
                subedition.Name,
                subedition.AnswerOpen,
                subedition.AnswerClose,
                subedition.State,
                subedition.CreatedAt,
                subedition.UpdatedAt,
                subedition.Version
            );
        }

        public async Task<SubEditionDto> GetSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
        {
            var subedition = await _db.SubEditions
                .AsNoTracking()
                .Include(e => e.Edition)
                .FirstOrDefaultAsync(e => e.Id == subedition_id && e.Edition.Id == edition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition not found");
            }

            return new SubEditionDto
            (
                subedition.Id,
                subedition.EditionId,
                subedition.Name,
                subedition.AnswerOpen,
                subedition.AnswerClose,
                subedition.State,
                subedition.CreatedAt,
                subedition.UpdatedAt,
                subedition.Version
            );
        }

        public async Task<(List<SubEditionDto>, ApiPagination)> GetSubEditionsAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, PageQueryDto pageQueryDto)
        {
            var query = _db.SubEditions
                .AsNoTracking()
                .Where(se => se.EditionId == edition_id);

            var totalCount = await query.CountAsync();

            var subeditions = await query
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new SubEditionDto(
                            e.Id,
                            e.EditionId,
                            e.Name,
                            e.AnswerOpen,
                            e.AnswerClose,
                            e.State,
                            e.CreatedAt,
                            e.UpdatedAt,
                            e.Version
                        ))
                        .ToListAsync();

            return (subeditions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task DeleteSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State == SubEditionStates.STATE_INACTIVE);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is active or closed can only delete the inactive subeditions");
            }

            _db.SubEditions.Remove(subedition);
            await _db.SaveChangesAsync();
        }

        public async Task<SubEditionDto> ActivateSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State != SubEditionStates.STATE_CLOSED);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is closed");
            }

            subedition.State = SubEditionStates.STATE_ACTIVE;
            await _db.SaveChangesAsync();

            return new SubEditionDto
            (
                subedition.Id,
                subedition.EditionId,
                subedition.Name,
                subedition.AnswerOpen,
                subedition.AnswerClose,
                subedition.State,
                subedition.CreatedAt,
                subedition.UpdatedAt,
                subedition.Version
            );
        }

        public async Task<SubEditionDto> CloseSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State == SubEditionStates.STATE_ACTIVE);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is not active");
            }

            subedition.State = SubEditionStates.STATE_CLOSED;
            await _db.SaveChangesAsync();

            return new SubEditionDto
            (
                subedition.Id,
                subedition.EditionId,
                subedition.Name,
                subedition.AnswerOpen,
                subedition.AnswerClose,
                subedition.State,
                subedition.CreatedAt,
                subedition.UpdatedAt,
                subedition.Version
            );
        }

        public async Task<TaskItemDto> CreateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, TaskItemCreateDto createTaskDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id);

            if (subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is not a part of the this edition");
            }

            var newTask = await _db.TaskItems.FirstOrDefaultAsync(t => t.Name == createTaskDto.Name && t.SubEditionId == subedition_id);

            if (newTask != null)
            {
                throw new CoreException(CoreErrorCodes.TaskAlreadyExsits, "Task with this name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            newTask = new TaskItem
            {
                Id = Guid.CreateVersion7(),
                SubEditionId = subedition_id,
                Name = createTaskDto.Name,
                ContentJson = createTaskDto.ContentJson,
                Tier = createTaskDto.Tier,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };


            await _db.TaskItems.AddAsync(newTask);
            await _db.SaveChangesAsync();

            return new TaskItemDto
            (
                newTask.Id,
                newTask.SubEditionId,
                newTask.Name,
                newTask.Tier,
                newTask.ContentJson,
                newTask.CreatedAt,
                newTask.UpdatedAt,
                newTask.Version
            );
        }

        public async Task DeleteTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
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

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionId == subedition_id && e.SubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            _db.TaskItems.Remove(task);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskItemDto> GetTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
        {
            var task = await _db.TaskItems
                .AsNoTracking()
                .Include(e => e.SubEdition)
                .ThenInclude(e => e.Edition)
                .FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionId == subedition_id && e.SubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new CoreException(CoreErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            return new TaskItemDto
            (
                task.Id,
                task.SubEditionId,
                task.Name,
                task.Tier,
                task.ContentJson,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version
            );
        }

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTaskItemsAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, PageQueryDto pageQueryDto)
        {
            var query = _db.TaskItems.AsNoTracking();

            var totalCount = await query.CountAsync();
            var entities = await query
                .Include(e => e.SubEdition)
                .ThenInclude(e => e.Edition)
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskItems = entities.Where(e => e.SubEdition.Id == subedition_id && e.SubEdition.Edition.Id == edition_id).Select(e => new TaskItemDto
            (
                e.Id,
                e.SubEditionId,
                e.Name,
                e.Tier,
                e.ContentJson,
                e.CreatedAt,
                e.UpdatedAt,
                e.Version
            )).ToList();

            return (TaskItems, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}