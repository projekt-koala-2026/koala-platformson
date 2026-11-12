using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Core.Services
{
    public class EditionService
    {
        private readonly CoreDbContext _db;
        private readonly IAccountModule _accountModule;

        public EditionService(CoreDbContext db, IAccountModule accountModule)
        {
            _db = db;
            _accountModule = accountModule;
        }

        public async Task<EditionDto> CreateEditionAsync(ClaimsPrincipal? claimsPrincipal, EditionCreateDto createEditionDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            if (createEditionDto.TeamRegisterOpen >= createEditionDto.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Registration opening must be before closing");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Name == createEditionDto.Name);

            if (edition != null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionAlreadyExists, "Edition with this name already exists");
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            bool isEditionWithNewNameAlreadyInDb = await _db.Editions.AnyAsync(e => e.Name == editionUpdateDto.Name && e.Id != id);

            if (isEditionWithNewNameAlreadyInDb == true)
            {
                throw new KoalaException(KoalaErrorCodes.EditionAlreadyExists, "Edition with this name already exist thus canot update this edition name");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition of provided id does not exist");
            }

            if (edition.Version != editionUpdateDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Editions versions do not match make sure u have the freshest data");
            }

            if (editionUpdateDto.TeamRegisterOpen >= editionUpdateDto.TeamRegisterClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Registration opening must be before closing");
            }

            edition.Name = editionUpdateDto.Name;
            edition.TeamRegisterOpen = editionUpdateDto.TeamRegisterOpen;
            edition.TeamRegisterClose = editionUpdateDto.TeamRegisterClose;
            edition.UpdatedAt = DateTime.UtcNow;
            edition.Version++;
            await _db.SaveChangesAsync();

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }
        public async Task<EditionDto> EditionActivateAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            bool isAnyEditionActive = await _db.Editions.AnyAsync(e => e.State == EditionStates.STATE_ACTIVE);
            if (isAnyEditionActive)
            {
                throw new KoalaException(KoalaErrorCodes.ActiveEditionAlreadyExists, "There already exists another editions that is curently active canot have two editions active at once");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State == EditionStates.STATE_INACTIVE);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be activated becouse it was closed or is active already");
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id && e.State == EditionStates.STATE_ACTIVE);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be closed becouse it is not active");
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
                        if (state != EditionStates.STATE_ACTIVE && state != EditionStates.STATE_INACTIVE && state != EditionStates.STATE_CLOSED)
                        {
                            throw new KoalaException(KoalaErrorCodes.EditionInvalidStates, "The states are not valid states");
                        }
                    }
                }

            }

            if (editionQueryDto.States.Count != 0)
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
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "There is no current active edition at the moment");
            }

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<SubEditionDto> CreateSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid id, SubEditionCreateDto createSubEditionDto)
        {

            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == id);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition does not exist");
            }

            if (edition.State == EditionStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Subeditions can only be created for the active and inactive edition");
            }

            if (createSubEditionDto.AnswerOpen >= createSubEditionDto.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Answer opening must be before closing");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Name == createSubEditionDto.Name && e.EditionId == id);

            if (subedition != null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionAlreadyExists, "SubEdition with this name already exists canot create a new one");
            }

            DateTime timeNow = DateTime.UtcNow;


            SubEdition newSubEdition = new SubEdition
            {
                Id = Guid.CreateVersion7(),
                EditionId = id,
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State != EditionStates.STATE_CLOSED);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition does not exist or is closed");
            }

            bool isSubEditionWithNewNameAlreadyInDb = await _db.SubEditions.AnyAsync(e => e.Name == updateSubEditionDto.Name && e.Id != subedition_id && e.EditionId == edition_id);

            if (isSubEditionWithNewNameAlreadyInDb == true)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionAlreadyExists, "SubEdition with this name already exist thus canot update this subedition name to it");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State != SubEditionStates.STATE_CLOSED);

            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is closed");
            }

            if (subedition.Version != updateSubEditionDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "SubEditions versions do not match make sure u have the freshest data");
            }

            if (updateSubEditionDto.AnswerOpen >= updateSubEditionDto.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Answer opening must be before closing");
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
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition not found");
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State == SubEditionStates.STATE_INACTIVE);

            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is active or closed can only delete the inactive subeditions");
            }

            _db.SubEditions.Remove(subedition);
            await _db.SaveChangesAsync();
        }

        public async Task<SubEditionDto> ActivateSubEditionAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State != SubEditionStates.STATE_CLOSED);

            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is closed");
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id && e.State == SubEditionStates.STATE_ACTIVE);

            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is not active");
            }

            if (DateTime.UtcNow <= subedition.AnswerClose)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot close the subedition before the answer deadline");

            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.SubEditionId == subedition_id &&
                (ts.Reviewer1Id == null || ts.Reviewer2Id == null || ts.Reviewer1Id == ts.Reviewer2Id ||
                 ts.Review1Score == null || ts.Review2Score == null || ts.Review1Score != ts.Review2Score ||
                 ts.AutoReviewState == null)))
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot close the subedition until all submitted answers have two matching reviews");

            subedition.State = SubEditionStates.STATE_CLOSED;
            subedition.UpdatedAt = DateTime.UtcNow;
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

        public async Task<TaskItemDto> CreateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, TaskItemCreateDto createTaskDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var subedition = await _db.SubEditions.Include(e => e.Edition).FirstOrDefaultAsync(e => e.Id == subedition_id && e.EditionId == edition_id);

            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "SubEdition of provided id does not exist or is not a part of the this edition");
            }

            if (subedition.State == SubEditionStates.STATE_CLOSED || subedition.Edition.State == EditionStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot add tasks to a closed subedition");
            }

            var newTask = await _db.TaskItems.FirstOrDefaultAsync(t => t.Name == createTaskDto.Name && t.SubEditionId == subedition_id);

            if (newTask != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAlreadyExists, "Task with this name already exists");
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

        public async Task<TaskItemDto> UpdateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id, TaskItemUpdateDto request)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can update tasks");
            if (string.IsNullOrWhiteSpace(request.Name) || (request.Tier != TaskItemTiers.TIER_1 && request.Tier != TaskItemTiers.TIER_2))
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Task name and a valid tier are required");
            try { using var content = System.Text.Json.JsonDocument.Parse(request.ContentJson); }
            catch (System.Text.Json.JsonException) { throw new KoalaException(KoalaErrorCodes.BadRequest, "Task content must be valid JSON"); }

            var task = await _db.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == task_id && t.SubEditionId == subedition_id && t.SubEdition.EditionId == edition_id);
            if (task == null)
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task does not exist in the specified subedition");
            if (await _db.TaskItems.AnyAsync(t => t.SubEditionId == subedition_id && t.Name == request.Name && t.Id != task_id))
                throw new KoalaException(KoalaErrorCodes.TaskAlreadyExists, "A task with this name already exists in the subedition");
            var now = DateTime.UtcNow;
            var updated = await _db.TaskItems.Where(t => t.Id == task_id && t.SubEditionId == subedition_id &&
                    t.SubEdition.EditionId == edition_id && t.SubEdition.State != SubEditionStates.STATE_CLOSED &&
                    t.Version == request.Version && !_db.TaskSubmissions.Any(s => s.TaskId == task_id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Name, request.Name)
                    .SetProperty(t => t.Tier, request.Tier).SetProperty(t => t.ContentJson, request.ContentJson)
                    .SetProperty(t => t.UpdatedAt, now).SetProperty(t => t.Version, t => t.Version + 1));
            if (updated == 0)
                throw new KoalaException(KoalaErrorCodes.Conflict, "The task changed, is closed, or already has submissions");
            return new TaskItemDto(task.Id, task.SubEditionId, request.Name, request.Tier, request.ContentJson, task.CreatedAt, now, request.Version + 1);
        }

        public async Task DeleteTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionId == subedition_id && e.SubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            var deleted = await _db.TaskItems.Where(t => t.Id == task_id && t.SubEditionId == subedition_id &&
                t.SubEdition.EditionId == edition_id && t.SubEdition.State != SubEditionStates.STATE_CLOSED &&
                !_db.TaskSubmissions.Any(s => s.TaskId == task_id)).ExecuteDeleteAsync();
            if (deleted == 0)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Task is closed or already has submissions");
        }

        public async Task<TaskItemDto> GetTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid task_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationRevier = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin && !isOrganizationRevier)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskItems
                .AsNoTracking()
                .Include(e => e.SubEdition)
                .ThenInclude(e => e.Edition)
                .FirstOrDefaultAsync(e => e.Id == task_id && e.SubEditionId == subedition_id && e.SubEdition.EditionId == edition_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");
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
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationRevier = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin && !isOrganizationRevier)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var query = _db.TaskItems.AsNoTracking()
                .Where(e => e.SubEditionId == subedition_id && e.SubEdition.EditionId == edition_id);

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

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTasksActiveAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid subedition_id, Guid team_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isCaptain = claimsPrincipal?.IsInRole("TEAM_CAPTAIN") == true;

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isCaptain)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var teamInfo = await _accountModule.Internal_GetTeamInfo(team_id);
            if (teamInfo == null || teamInfo.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a Team Player that is a captain can use this endpoint");
            }

            if (teamInfo.EditionId != edition_id)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "The team does not belong to this edition");
            }

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subedition_id);
            if(subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if(subedition.AnswerOpen > timeNow || timeNow > subedition.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Teams cant in this time answer/view tasks");
            }
            
            var query = _db.TaskItems.AsNoTracking()
                .Where(e => e.SubEditionId == subedition_id
                        && e.SubEdition.State == "ACTIVE"
                        && e.SubEdition.EditionId == edition_id
                        && e.SubEdition.Edition.State == "ACTIVE");

            if(teamInfo.Tier == TaskItemTiers.TIER_1)
            {
                query = query.Where(e => e.Tier == TaskItemTiers.TIER_1);
            }
            if(teamInfo.Tier == TaskItemTiers.TIER_2)
            {
                query = query.Where(e => e.Tier == TaskItemTiers.TIER_1 || e.Tier == TaskItemTiers.TIER_2);
            }

            var totalCount = await query.CountAsync();
            
            var taskItems = await query
                .OrderBy(e => e.CreatedAt)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new TaskItemDto(
                    e.Id,
                    e.SubEditionId,
                    e.Name,
                    e.Tier,
                    e.ContentJson,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                ))
                .ToListAsync();

            return (taskItems, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}
