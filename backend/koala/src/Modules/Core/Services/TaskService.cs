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

        private async Task SaveSubmissionsAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "The submission was changed by another user. Refresh and try again.");
            }
        }

        public async Task<TaskItemDto> CreateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid etap1series_id, TaskItemCreateDto request)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == etap1series_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            DateTime timeNow = DateTime.UtcNow;
            var task = new TaskItem
            {
                Id = Guid.CreateVersion7(),
                Etap1SeriesId = etap1series_id,
                Name = request.Name,
                IsPrimarySchoolTask = request.IsPrimarySchoolTask,
                IsSecondarySchoolTask = request.IsSecondarySchoolTask,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var task_parts = new List<TaskPart>();
            var task_answer = new List<TaskPartAnswer>();
            foreach(var part in request.TaskPartCreateDtos)
            {
                var taskPart = new TaskPart
                {
                    Id = Guid.CreateVersion7(),
                    TaskItemId = task.Id,
                    Name = part.Name,
                    Description = part.Description,
                    MaxSubmissionLength = part.MaxSubmissionLength,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
                task_parts.Add
                (
                    taskPart   
                );

                task_answer.AddRange(part.TaskAnswerCreateDtos.Select
                (
                    ta => new TaskPartAnswer
                    {
                        Id = Guid.CreateVersion7(),
                        TaskPartId = taskPart.Id, 
                        Answer = ta.Answer,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    }
                ).ToList());
            }

            await _db.TaskItems.AddAsync(task);
            await _db.TaskParts.AddRangeAsync(task_parts);
            await _db.TaskAnswers.AddRangeAsync(task_answer);

            await _db.SaveChangesAsync();

            return new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.IsPrimarySchoolTask,
                task.IsSecondarySchoolTask,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version,
                task_parts.Select(
                    tp => new TaskPartDto
                    (
                        tp.Id,
                        tp.TaskItemId,
                        tp.Name,
                        tp.Description,
                        tp.MaxSubmissionLength,
                        tp.CreatedAt,
                        tp.UpdatedAt,
                        tp.Version,
                        tp.TaskAnswers.Select
                        (
                            ta => new TaskAnswerDto
                            (
                                ta.Id,
                                ta.TaskPartId,
                                ta.Answer,
                                ta.CreatedAt,
                                ta.UpdatedAt,
                                ta.Version
                            )
                        ).ToList()
                    )   
                ).ToList()
            );
        }

        public async Task<TaskItemDto> UpdateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid etap1series_id, Guid task_item_id, TaskItemUpdateDto request)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == etap1series_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            var task = await _db.TaskItems.Include(ti => ti.TaskParts).ThenInclude(tp => tp.TaskAnswers).FirstOrDefaultAsync(ti => ti.Id == task_item_id && ti.Etap1SeriesId == etap1series_id);

            if(task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task with this id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            task.Name = request.Name;
            task.IsPrimarySchoolTask = request.IsPrimarySchoolTask;
            task.IsSecondarySchoolTask = request.IsSecondarySchoolTask;
            task.UpdatedAt = timeNow;
            task.Version++;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "The task item was changed by another user. Refresh and try again.");
            }

            return new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.IsPrimarySchoolTask,
                task.IsSecondarySchoolTask,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version,
                task.TaskParts.Select(
                    tp => new TaskPartDto
                    (
                        tp.Id,
                        tp.TaskItemId,
                        tp.Name,
                        tp.Description,
                        tp.MaxSubmissionLength,
                        tp.CreatedAt,
                        tp.UpdatedAt,
                        tp.Version,
                        tp.TaskAnswers.Select
                        (
                            ta => new TaskAnswerDto
                            (
                                ta.Id,
                                ta.TaskPartId,
                                ta.Answer,
                                ta.CreatedAt,
                                ta.UpdatedAt,
                                ta.Version
                            )
                        ).ToList()
                    )   
                ).ToList()
            );
        }

        public async Task DeleteTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid etap1series_id, Guid task_item_id)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == etap1series_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            var task = await _db.TaskItems.Include(ti => ti.TaskParts).ThenInclude(tp => tp.TaskAnswers).FirstOrDefaultAsync(ti => ti.Id == task_item_id && ti.Etap1SeriesId == etap1series_id);

            if(task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task with this id does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;

            _db.TaskItems.Remove(task);

            await _db.SaveChangesAsync();

            return;
        }

        public async Task<TaskItemDto> GetTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid etap1series_id, Guid task_item_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin && !isOrganizationReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == etap1series_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            var task = await _db.TaskItems.Include(ti => ti.TaskParts).ThenInclude(tp => tp.TaskAnswers).AsNoTracking().FirstOrDefaultAsync(ti => ti.Id == task_item_id && ti.Etap1SeriesId == etap1series_id);

            if(task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task with this id does not exist");
            }

            return new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.IsPrimarySchoolTask,
                task.IsSecondarySchoolTask,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version,
                task.TaskParts.Select(
                    tp => new TaskPartDto
                    (
                        tp.Id,
                        tp.TaskItemId,
                        tp.Name,
                        tp.Description,
                        tp.MaxSubmissionLength,
                        tp.CreatedAt,
                        tp.UpdatedAt,
                        tp.Version,
                        tp.TaskAnswers.Select
                        (
                            ta => new TaskAnswerDto
                            (
                                ta.Id,
                                ta.TaskPartId,
                                ta.Answer,
                                ta.CreatedAt,
                                ta.UpdatedAt,
                                ta.Version
                            )
                        ).ToList()
                    )   
                ).ToList()
            );
        }

        public async Task<(List<TaskItemDto>, ApiPagination)> GetTaskItemsAsync(ClaimsPrincipal? claimsPrincipal, Guid etap1series_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin && !isOrganizationReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == etap1series_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            var tasks = await _db.TaskItems.Include(ti => ti.TaskParts).ThenInclude(tp => tp.TaskAnswers).AsNoTracking()
                .Where(ti => ti.Etap1SeriesId == etap1series_id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            return (tasks.Select(task => new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.IsPrimarySchoolTask,
                task.IsSecondarySchoolTask,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version,
                task.TaskParts.Select(
                    tp => new TaskPartDto
                    (
                        tp.Id,
                        tp.TaskItemId,
                        tp.Name,
                        tp.Description,
                        tp.MaxSubmissionLength,
                        tp.CreatedAt,
                        tp.UpdatedAt,
                        tp.Version,
                        tp.TaskAnswers.Select
                        (
                            ta => new TaskAnswerDto
                            (
                                ta.Id,
                                ta.TaskPartId,
                                ta.Answer,
                                ta.CreatedAt,
                                ta.UpdatedAt,
                                ta.Version
                            )
                        ).ToList()
                    )   
                ).ToList()
            )).ToList(),
            new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, await _db.TaskItems.CountAsync(ti => ti.Etap1SeriesId == etap1series_id))
            );
        }

        public async Task<(List<TaskItemDto>, ApiPagination)> GetActiveTaskItemsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            DateTime timeNow = DateTime.UtcNow;
            var Etap1Series = await _db.Etap1Series.Where(e => e.State == LifecycleStates.STATE_ACTIVE && e.AnswerOpen <= timeNow && e.AnswerClose > timeNow).ToListAsync();

            if (!Etap1Series.Any())
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "There is no active etap1series at this time with open window for tasks");
            }

            var tasks = await _db.TaskItems.Include(ti => ti.TaskParts).ThenInclude(tp => tp.TaskAnswers).AsNoTracking()
                .Where(ti => Etap1Series.Any(e1s => e1s.Id == ti.Etap1SeriesId))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            return (tasks.Select(task => new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.IsPrimarySchoolTask,
                task.IsSecondarySchoolTask,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version,
                task.TaskParts.Select(
                    tp => new TaskPartDto
                    (
                        tp.Id,
                        tp.TaskItemId,
                        tp.Name,
                        tp.Description,
                        tp.MaxSubmissionLength,
                        tp.CreatedAt,
                        tp.UpdatedAt,
                        tp.Version,
                        null
                    )   
                ).ToList()
            )).ToList(),
            new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, await _db.TaskItems.CountAsync(ti => Etap1Series.Any(e1s => e1s.Id == ti.Etap1SeriesId)))
            );
        }

        public async Task<TaskPartDto> CreateTaskPartAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, TaskPartCreateDto request)
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

            var task = await _db.TaskItems.Include(ti => ti.TaskParts).Include(ti => ti.Etap1Series).FirstOrDefaultAsync(ti => ti.Id == task_item_id && ti.TaskParts.All(tp => tp.Name != request.Name));

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task item with this id does not exist or it already has a task part with this name");
            }
            if (task.Etap1Series.State == LifecycleStates.STATE_CLOSED)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot modify task parts in a closed Etap1Series");

            DateTime timeNow = DateTime.UtcNow;

            var taskPart = new TaskPart
            {
                Id = Guid.CreateVersion7(),
                TaskItemId = task_item_id,
                Name = request.Name,
                Description = request.Description,
                MaxSubmissionLength = request.MaxSubmissionLength,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var taskAnswers = new List<TaskPartAnswer>();
            foreach(var answer in request.TaskAnswerCreateDtos)
            {
                taskAnswers.Add
                (
                    new TaskPartAnswer
                    {
                        Id = Guid.CreateVersion7(),
                        TaskPartId = taskPart.Id,
                        Answer = answer.Answer,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    }
                );
            }
            await _db.TaskParts.AddAsync(taskPart);
            await _db.TaskAnswers.AddRangeAsync(taskAnswers);
            await _db.SaveChangesAsync();

            return new TaskPartDto
            (
                taskPart.Id,
                taskPart.TaskItemId,
                taskPart.Name,
                taskPart.Description,
                taskPart.MaxSubmissionLength,
                taskPart.CreatedAt,
                taskPart.UpdatedAt,
                taskPart.Version,
                taskAnswers.Select
                (
                    ta => new TaskAnswerDto
                    (
                        ta.Id,
                        ta.TaskPartId,
                        ta.Answer,
                        ta.CreatedAt,
                        ta.UpdatedAt,
                        ta.Version
                    )
                ).ToList()
            );
        }

        public async Task<TaskPartDto> UpdateTaskPartAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, Guid task_part_id, TaskPartUpdateDto request)
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

            var taskPart = await _db.TaskParts.Include(tp => tp.TaskAnswers).FirstOrDefaultAsync(tp => tp.Id == task_part_id && tp.TaskItemId == task_item_id);

            if (taskPart == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task part with this id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            if(await _db.TaskParts.AnyAsync(tp => tp.TaskItemId == task_item_id && tp.Name == request.Name && tp.Id != task_part_id))
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task part with this name already exists cannot update the curent one");
            }

            DateTime timeNow = DateTime.UtcNow;

            taskPart.Name = request.Name;
            taskPart.Description = request.Description;
            taskPart.MaxSubmissionLength = request.MaxSubmissionLength;
            taskPart.UpdatedAt = timeNow;
            taskPart.Version++;

            try{
                await _db.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "This task part was updated refresh ur data and try agein");
            }
            return new TaskPartDto
            (
                taskPart.Id,
                taskPart.TaskItemId,
                taskPart.Name,
                taskPart.Description,
                taskPart.MaxSubmissionLength,
                taskPart.CreatedAt,
                taskPart.UpdatedAt,
                taskPart.Version,
                taskPart.TaskAnswers.Select
                (
                    ta => new TaskAnswerDto
                    (
                        ta.Id,
                        ta.TaskPartId,
                        ta.Answer,
                        ta.CreatedAt,
                        ta.UpdatedAt,
                        ta.Version
                    )
                ).ToList()
            );
        }

        public async Task DeleteTaskPartAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, Guid task_part_id)
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

            var taskPart = await _db.TaskParts.Include(tp => tp.TaskAnswers).FirstOrDefaultAsync(tp => tp.Id == task_part_id && tp.TaskItemId == task_item_id);

            if (taskPart == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task part with this id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            _db.TaskParts.Remove(taskPart);

            await _db.SaveChangesAsync();
            
            return;
        }

        public async Task<TaskAnswerDto> CreateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_part_id, TaskAnswerCreateDto createTaskAnswerDto)
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

            var task = await _db.TaskParts.FirstOrDefaultAsync(e => e.Id == task_part_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task part of provided id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            var newTaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == createTaskAnswerDto.Answer && tc.TaskPart.Id == task_part_id);

            if (newTaskAnswer != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerAlreadyExists, "Task answer for this task with this answer already exists");
            }

            DateTime timeNow = DateTime.UtcNow;
            newTaskAnswer = new TaskPartAnswer
            {
                Id = Guid.CreateVersion7(),
                TaskPartId = task_part_id,
                Answer = createTaskAnswerDto.Answer,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            await _db.TaskAnswers.AddAsync(newTaskAnswer);
            await _db.SaveChangesAsync();

            return new TaskAnswerDto
            (
                newTaskAnswer.Id,
                newTaskAnswer.TaskPartId,
                newTaskAnswer.Answer,
                newTaskAnswer.CreatedAt,
                newTaskAnswer.UpdatedAt,
                newTaskAnswer.Version
            );
        }

        public async Task<TaskAnswerDto> UpdateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_part_id, Guid task_answer_id, TaskAnswerUpdateDto updateTaskAnswerDto)
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

            var task = await _db.TaskParts.FirstOrDefaultAsync(e => e.Id == task_part_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task part of provided id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            var TaskAnswerWithThisNameExists = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == updateTaskAnswerDto.Answer && tc.TaskPartId == task_part_id && tc.Id != task_answer_id);

            if (TaskAnswerWithThisNameExists != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerAlreadyExists, "task answer for this task part with this answer already exists thus canot change this task answer to be the same ! Duplication");
            }

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Id == task_answer_id && tc.TaskPartId == task_part_id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided task_answer_id and task_part_id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            if (TaskAnswer.Version != updateTaskAnswerDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "TaskAnswer versions do not match make sure u have the freshest data");
            }

            TaskAnswer.Answer = updateTaskAnswerDto.Answer;
            TaskAnswer.UpdatedAt = DateTime.UtcNow;
            TaskAnswer.Version++;

            await _db.SaveChangesAsync();

            return new TaskAnswerDto
            (
                TaskAnswer.Id,
                TaskAnswer.TaskPartId,
                TaskAnswer.Answer,
                TaskAnswer.CreatedAt,
                TaskAnswer.UpdatedAt,
                TaskAnswer.Version
            );
        }

        public async Task DeleteTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_part_id, Guid task_answer_id)
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

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskPartId == task_part_id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }
            await EnsureTaskPartSeriesIsOpenAsync(task_part_id);

            _db.TaskAnswers.Remove(TaskAnswer);

            await _db.SaveChangesAsync();
        }

        public async Task<TaskAnswerDto> GetTaskPartAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_part_id, Guid task_answer_id)
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

            var TaskAnswer = await _db.TaskAnswers
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskPartId == task_part_id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }

            return new TaskAnswerDto
            (
                TaskAnswer.Id,
                TaskAnswer.TaskPartId,
                TaskAnswer.Answer,
                TaskAnswer.CreatedAt,
                TaskAnswer.UpdatedAt,
                TaskAnswer.Version
            );
        }

        public async Task<(List<TaskAnswerDto> Data, ApiPagination Pagination)> GetTaskPartAnswersAsync(ClaimsPrincipal? claimsPrincipal, Guid task_part_id, PageQueryDto pageQueryDto)
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

            var query = _db.TaskAnswers.Include(ta => ta.TaskPart).Where(tc => tc.TaskPartId == task_part_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TaskPart.TaskItemId)
                .ThenBy(e => e.TaskPartId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(ta => new TaskAnswerDto
                (
                    ta.Id,
                    ta.TaskPartId,
                    ta.Answer,
                    ta.CreatedAt,
                    ta.UpdatedAt,
                    ta.Version
                ))
                .ToListAsync();

            return (entities, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskSubmissionDto> UpdateTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1Series_id, Guid task_part_id, Guid team_id, UpsertTaskSubmissionDto submitTaskSubmissionDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            bool isTeamPlayer = ClaimsHelper.IsRoleCaptain(claimsPrincipal);
            if (!isTeamPlayer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a Team Player that is a captain can use this endpoint");
            }

            var teamInfo = await _accountModule.Internal_GetTeamInfo(team_id);
            if (teamInfo == null || teamInfo.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only a Team Player that is a captain can use this endpoint");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1Series_id);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");
            }

            if (teamInfo.EditionId != Etap1Series.EditionId)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "The team does not belong to this edition");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (Etap1Series.AnswerOpen > timeNow || timeNow > Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Teams cannot answer tasks outside the answer period");
            }

            var task = await _db.TaskParts.Include(t => t.TaskItem).ThenInclude(ti => ti.Etap1Series)
                .FirstOrDefaultAsync(t => t.Id == task_part_id && t.TaskItem.Etap1SeriesId == Etap1Series_id);
            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task does not belong to the specified Etap1Series");
            }

            if(task.MaxSubmissionLength < submitTaskSubmissionDto.Submission.Length || submitTaskSubmissionDto.Submission.Length == 0)
            {
                throw new KoalaException(KoalaErrorCodes.TaskSubmissionNotFound, "Answer length not permited");
            }

            if (task.TaskItem.Etap1Series.State != LifecycleStates.STATE_ACTIVE ||
                !((task.TaskItem.IsPrimarySchoolTask == true && (teamInfo.Tier == CompetitionTiers.TIER_1 || teamInfo.Tier == CompetitionTiers.TIER_2)) ||
                  (task.TaskItem.IsSecondarySchoolTask == true && teamInfo.Tier == CompetitionTiers.TIER_2)))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "This task is not available to the team");
            }

            TaskPartSubmission? submission = null;
            if (teamInfo.Tier == CompetitionTiers.TIER_1)
            {
                submission = await _db.TaskSubmissions.Include(e => e.TaskPart).ThenInclude(t => t.TaskItem).FirstOrDefaultAsync(e => e.TaskPartId == task_part_id && e.TaskPart.TaskItem.IsPrimarySchoolTask && e.TeamId == team_id);
            }
            if (teamInfo.Tier == CompetitionTiers.TIER_2)
            {
                submission = await _db.TaskSubmissions.Include(e => e.TaskPart).ThenInclude(t => t.TaskItem).FirstOrDefaultAsync(e => e.TaskPartId == task_part_id && (e.TaskPart.TaskItem.IsPrimarySchoolTask || e.TaskPart.TaskItem.IsSecondarySchoolTask) && e.TeamId == team_id);
            }

            if (submission == null)
            {
                submission = new TaskPartSubmission
                {
                    TeamId = team_id,
                    TaskPartId = task_part_id,
                    Review1Score = null,
                    Review2Score = null,
                    Reviewer1Id = null,
                    Reviewer2Id = null,
                    AutoReviewState = null,
                    State = TaskSubmissionStates.STATE_UNCHECKED,
                    Submission = submitTaskSubmissionDto.Submission,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 0,
                };

                await _db.TaskSubmissions.AddAsync(submission);
            }

            if (submission.Version != submitTaskSubmissionDto.Version && submission.Version != 0)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "U cant update when having stale data");
            }

            submission.Submission = submitTaskSubmissionDto.Submission;
            submission.UpdatedAt = timeNow;
            submission.Version++;

            await SaveSubmissionsAsync();

            return new TaskSubmissionDto
            (
                submission.TeamId,
                task.TaskItemId,
                submission.TaskPartId,
                null,
                null,
                null,
                null,
                null,
                submission.Submission,
                submission.State,
                submission.CreatedAt,
                submission.UpdatedAt,
                submission.Version
            );
        }

        public async Task<TaskSubmissionDto> GetTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1Series_id, Guid task_part_id, Guid team_id)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleCaptain(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var team = await _accountModule.Internal_GetTeamInfo(team_id);
            if (team == null || team.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var timeNow = DateTime.UtcNow;
            var submission = await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).ThenInclude(ti => ti.Etap1Series).AsNoTracking()
                .FirstOrDefaultAsync(s => s.TeamId == team_id && s.TaskPartId == task_part_id &&
                    s.TaskPart.TaskItem.Etap1SeriesId == Etap1Series_id && s.TaskPart.TaskItem.Etap1Series.EditionId == team.EditionId &&
                    s.TaskPart.TaskItem.Etap1Series.State == LifecycleStates.STATE_ACTIVE &&
                    s.TaskPart.TaskItem.Etap1Series.Edition.State == LifecycleStates.STATE_ACTIVE &&
                    s.TaskPart.TaskItem.Etap1Series.AnswerOpen <= timeNow && s.TaskPart.TaskItem.Etap1Series.AnswerClose >= timeNow &&
                    (((s.TaskPart.TaskItem.IsPrimarySchoolTask == true) && (team.Tier == CompetitionTiers.TIER_1 || team.Tier == CompetitionTiers.TIER_2)) ||
                    ((s.TaskPart.TaskItem.IsSecondarySchoolTask == true) && (team.Tier == CompetitionTiers.TIER_2))));
            if (submission == null)
                throw new KoalaException(KoalaErrorCodes.TaskSubmissionNotFound, "Submission for this team and task does not exist");

            return new TaskSubmissionDto(submission.TeamId, submission.TaskPart.TaskItemId, submission.TaskPartId, null, null, null, null, null,
                submission.Submission, submission.State, submission.CreatedAt, submission.UpdatedAt, submission.Version);
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTaskSubmissionsTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1Series_id, Guid team_id, PageQueryDto pageQueryDto)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleCaptain(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var team = await _accountModule.Internal_GetTeamInfo(team_id);
            if (team == null || team.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var timeNow = DateTime.UtcNow;
            var query = _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).ThenInclude(ti => ti.Etap1Series).AsQueryable().AsNoTracking()
                .Where(s => s.TeamId == team_id &&
                s.TaskPart.TaskItem.Etap1SeriesId == Etap1Series_id && s.TaskPart.TaskItem.Etap1Series.EditionId == team.EditionId &&
                s.TaskPart.TaskItem.Etap1Series.State == LifecycleStates.STATE_ACTIVE &&
                s.TaskPart.TaskItem.Etap1Series.Edition.State == LifecycleStates.STATE_ACTIVE &&
                s.TaskPart.TaskItem.Etap1Series.AnswerOpen <= timeNow && s.TaskPart.TaskItem.Etap1Series.AnswerClose >= timeNow &&
                (((s.TaskPart.TaskItem.IsPrimarySchoolTask == true) && (team.Tier == CompetitionTiers.TIER_1 || team.Tier == CompetitionTiers.TIER_2)) ||
                ((s.TaskPart.TaskItem.IsSecondarySchoolTask == true) && (team.Tier == CompetitionTiers.TIER_2))));
            var sCount = await query.CountAsync();
            var submissions = await query.OrderBy(e => e.TaskPart.TaskItemId)
                .ThenBy(e => e.TaskPartId).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToListAsync();
            if (!submissions.Any())
                throw new KoalaException(KoalaErrorCodes.TaskSubmissionNotFound, "Submission for this team and task does not exist");

            return (
                submissions.Select
                (
                    s => new TaskSubmissionDto(
                        s.TeamId,
                        s.TaskPart.TaskItemId,
                        s.TaskPartId,
                        null,
                        null,
                        null,
                        null,
                        null,
                        s.Submission,
                        null,
                        s.CreatedAt,
                        s.UpdatedAt,
                        s.Version
                    )
                ).ToList()
                ,
                new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, sCount)
            );
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTaskItemSubmissionsAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, PageQueryDto pageQueryDto)
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

            var query = _db.TaskSubmissions.Include(t => t.TaskPart).Where(t => t.TaskPart.TaskItemId == task_item_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TaskPart.TaskItemId)
                .ThenBy(e => e.TaskPartId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new TaskSubmissionDto
                (
                    e.TeamId,
                    e.TaskPart.TaskItemId,
                    e.TaskPartId,
                    e.Reviewer1Id,
                    e.Reviewer2Id,
                    e.AutoReviewState,
                    e.Review1Score,
                    e.Review2Score,
                    e.Submission,
                    e.State,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                ))
                .ToListAsync();

            return (entities, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
        public async Task<TaskSubmissionDto> UpsertScoreByReviewerAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, Guid task_part_id, Guid team_id, Guid Etap1Series_id, UpsertReviewDto upsertReviewDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            if (reviewer_id != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Reviewer identity does not match the current user");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1Series_id && se.State == LifecycleStates.STATE_ACTIVE);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist or is inactive or closed");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot score submissions before the answer period ends");
            }

            var submission = await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).FirstOrDefaultAsync(e => e.TaskPartId == task_part_id && e.TeamId == team_id && e.TaskPart.TaskItem.Etap1SeriesId == Etap1Series_id);

            if (submission == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskSubmissionNotFound, "Task submission of provided submission_id does not exist");
            }

            var previousScore = submission.Reviewer1Id == reviewer_id ? submission.Review1Score : submission.Review2Score;
            if (submission.Reviewer1Id == reviewer_id)
            {
                submission.Review1Score = upsertReviewDto.Score;
                if (submission.Review1Score == submission.Review2Score && submission.Review2Score != null)
                {
                    submission.AutoReviewState = submission.Review1Score;
                }
            }
            else if (submission.Reviewer2Id == reviewer_id)
            {
                submission.Review2Score = upsertReviewDto.Score;
                if (submission.Review1Score == submission.Review2Score && submission.Review1Score != null)
                {
                    submission.AutoReviewState = submission.Review2Score;
                }
            }
            else
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Reviewer is not assigned to this submission");
            }

            if (submission.Review1Score != null && submission.Review2Score != null && submission.Review1Score != submission.Review2Score)
            {
                submission.AutoReviewState = null;
            }

            submission.UpdatedAt = DateTime.UtcNow;
            submission.Version++;

            await SaveSubmissionsAsync();

            return new TaskSubmissionDto
            (
                submission.TeamId,
                submission.TaskPart.TaskItemId,
                submission.TaskPartId,
                submission.Reviewer1Id,
                submission.Reviewer2Id,
                submission.AutoReviewState,
                submission.Review1Score,
                submission.Review2Score,
                submission.Submission,
                submission.State,
                submission.CreatedAt,
                submission.UpdatedAt,
                submission.Version
            );
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTasksToReviewAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            if (reviewer_id != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Reviewer identity does not match the current user");
            }

            var query = _db.TaskSubmissions.Include(ts => ts.TaskPart).Where(t => t.Reviewer1Id == reviewer_id || t.Reviewer2Id == reviewer_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TaskPart.TaskItemId)
                .ThenBy(e => e.TaskPartId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskSubmissions = entities.Select(e =>
            {
                return new TaskSubmissionDto(
                    e.TeamId,
                    e.TaskPart.TaskItemId,
                    e.TaskPartId,
                    e.Reviewer1Id,
                    e.Reviewer2Id,
                    e.AutoReviewState,
                    e.Review1Score,
                    e.Review2Score,
                    e.Submission,
                    e.State,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                );
            }).ToList();

            return (TaskSubmissions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<AutoCheckProgressDto> GetEtap1SeriesAutoCheckProgressAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1SeriesId)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can view automatic checking progress");
            if (!await _db.Etap1Series.AnyAsync(s => s.Id == Etap1SeriesId))
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");

            return await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskAnswers).Include(ts => ts.TaskPart).ThenInclude(ts => ts.TaskItem).AsNoTracking()
                .Where(s => s.TaskPart.TaskItem.Etap1SeriesId == Etap1SeriesId)
                .GroupBy(s => 1)
                .Select(g => new AutoCheckProgressDto(
                    g.Count(),
                    g.Count(s => s.AutoReviewState == null && s.Review1Score == null && s.Review2Score == null && s.TaskPart.TaskAnswers.Any()),
                    g.Count(s => s.AutoReviewState == null && s.Review1Score == null && s.Review2Score == null && !s.TaskPart.TaskAnswers.Any()),
                    g.Count(s => s.AutoReviewState == null && (s.Review1Score != null || s.Review2Score != null))))
                .FirstOrDefaultAsync() ?? new AutoCheckProgressDto(0, 0, 0, 0);
        }

        public async Task AutoCheckEtap1SeriesubmissionsAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1Series_id)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1Series_id);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");
            }

            if (DateTime.UtcNow <= Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot check submissions before the answer period ends");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            
            var submissions = await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskAnswers)
                .Where(ts => ts.TaskPart.TaskItem.Etap1SeriesId == Etap1Series_id && !string.IsNullOrEmpty(ts.Submission) && ts.TaskPart.TaskAnswers.Any()).ToListAsync();

            foreach(var s in submissions)
            {
                bool isCorrect = s.TaskPart.TaskAnswers.Any(ta =>
                    string.Equals(
                        ta.Answer?.Trim(),
                        s.Submission?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                s.AutoReviewState = isCorrect;
                s.UpdatedAt = DateTime.UtcNow;
                s.Version++;
            }

            await SaveSubmissionsAsync();
            await transaction.CommitAsync();
            return;
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTasksWithConflictsAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            if (reviewer_id != ClaimsHelper.GetUserGuid(claimsPrincipal))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Reviewer identity does not match the current user");
            }

            var query = _db.TaskSubmissions.Include(ts => ts.TaskPart).Where(t => (t.Reviewer1Id == reviewer_id || t.Reviewer2Id == reviewer_id) && t.Review1Score != t.Review2Score && t.Review1Score != null && t.Review2Score != null).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TaskPart.TaskItemId)
                .ThenBy(e => e.TaskPartId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskSubmissions = entities.Select(e =>
            {
                return new TaskSubmissionDto(
                    e.TeamId,
                    e.TaskPart.TaskItemId,
                    e.TaskPartId,
                    e.Reviewer1Id,
                    e.Reviewer2Id,
                    e.AutoReviewState,
                    e.Review1Score,
                    e.Review2Score,
                    e.Submission,
                    e.State,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                );
            }).ToList();

            return (TaskSubmissions, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        private async Task EnsureTaskPartSeriesIsOpenAsync(Guid taskPartId)
        {
            var isClosed = await _db.TaskParts
                .Include(tp => tp.TaskItem)
                .ThenInclude(ti => ti.Etap1Series)
                .Where(tp => tp.Id == taskPartId)
                .Select(tp => tp.TaskItem.Etap1Series.State == LifecycleStates.STATE_CLOSED)
                .FirstAsync();
            if (isClosed)
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Cannot modify task parts or answers in a closed Etap1Series");
        }

        private record ReviewGroup(Guid? Reviewer1Id, Guid? Reviewer2Id, int Total, int Reviewer1Reviewed, int Reviewer2Reviewed, int PendingSubmissions);

        private async Task<List<ReviewGroup>> GetReviewGroupsAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_Id)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can view review progress");
            if (!await _db.TaskItems.AnyAsync(t => t.Id == task_item_Id))
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");

            return await _db.TaskSubmissions.Include(ts => ts.TaskPart).AsNoTracking().Where(s => s.TaskPart.TaskItemId == task_item_Id)
                .GroupBy(s => new { s.Reviewer1Id, s.Reviewer2Id })
                .Select(g => new ReviewGroup(g.Key.Reviewer1Id, g.Key.Reviewer2Id, g.Count(),
                    g.Count(s => s.Review1Score != null), g.Count(s => s.Review2Score != null),
                    g.Count(s => s.Review1Score == null || s.Review2Score == null)))
                .ToListAsync();
        }

        public async Task<TaskReviewProgressDto> GetTaskItemReviewProgressAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_Id)
        {
            var groups = await GetReviewGroupsAsync(claimsPrincipal, task_item_Id);
            return new TaskReviewProgressDto(groups.Sum(g => g.Total),
                groups.Where(g => g.Reviewer1Id == null && g.Reviewer2Id == null).Sum(g => g.Total),
                groups.Where(g => (g.Reviewer1Id == null) != (g.Reviewer2Id == null)).Sum(g => g.Total),
                groups.Where(g => g.Reviewer1Id != null && g.Reviewer2Id != null)
                    .Select(g => new ReviewPairProgressDto(g.Reviewer1Id!.Value, g.Reviewer2Id!.Value, g.Total,
                        g.Reviewer1Reviewed, g.Reviewer2Reviewed, g.Total - g.Reviewer1Reviewed,
                        g.Total - g.Reviewer2Reviewed, g.PendingSubmissions)).ToList());
        }

        public async Task<List<PendingReviewerDto>> GetTaskItemPendingReviewersAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_Id)
        {
            var groups = await GetReviewGroupsAsync(claimsPrincipal, task_item_Id);
            var slots = groups.SelectMany(g => new[] {
                (Reviewer: g.Reviewer1Id, Other: g.Reviewer2Id, Pending: g.Total - g.Reviewer1Reviewed),
                (Reviewer: g.Reviewer2Id, Other: g.Reviewer1Id, Pending: g.Total - g.Reviewer2Reviewed)
            }).Where(s => s.Reviewer != null && s.Pending > 0);
            return slots.GroupBy(s => s.Reviewer!.Value).Select(g => new PendingReviewerDto(g.Key,
                g.Sum(s => s.Pending), g.Where(s => s.Other != null).Select(s => s.Other!.Value).Distinct().ToList())).ToList();
        }

        public async Task AssignReviewersAsync(ClaimsPrincipal? claimsPrincipal, Guid Etap1SeriesId, Guid task_item_Id, AssignReviewersDto taskReviewerPairs)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1SeriesId);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot assign reviewers before the answer period ends");
            }

            if (taskReviewerPairs.Reviewer1Id == taskReviewerPairs.Reviewer2Id)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Invalid pair provided");
            }

            var validReviewers = await _accountModule.Internal_GetReviewerIdsAsync([taskReviewerPairs.Reviewer1Id, taskReviewerPairs.Reviewer2Id]);
            if (validReviewers.Count != 2)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Every assigned reviewer must be a verified user with the reviewer role");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var hasDifferentAssignments = await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).AnyAsync(s =>
                s.TaskPart.TaskItemId == task_item_Id && s.TaskPart.TaskItem.Etap1SeriesId == Etap1SeriesId &&
                (s.Reviewer1Id != null || s.Reviewer2Id != null) &&
                !((s.Reviewer1Id == taskReviewerPairs.Reviewer1Id && s.Reviewer2Id == taskReviewerPairs.Reviewer2Id) ||
                  (s.Reviewer1Id == taskReviewerPairs.Reviewer2Id && s.Reviewer2Id == taskReviewerPairs.Reviewer1Id)));
            if (hasDifferentAssignments)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Existing submissions have a different or incomplete reviewer assignment");

            var submissions = await _db.TaskSubmissions
                .Include(s => s.TaskPart)
                .ThenInclude(tp => tp.TaskItem)
                .Where(s => s.TaskPart.TaskItem.Etap1SeriesId == Etap1SeriesId && s.TaskPart.TaskItemId == task_item_Id && s.Reviewer1Id == null && s.Reviewer2Id == null)
                .OrderBy(s => s.TaskPart.TaskItemId)
                .ThenBy(s => s.TaskPartId)
                .ToListAsync();

            if (!submissions.Any())
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "did not found un asigned tasks that can be given to the reviewer");
            }

            var now = DateTime.UtcNow;

            foreach (var sub in submissions)
            {
                sub.Reviewer1Id = taskReviewerPairs.Reviewer1Id;
                sub.Reviewer2Id = taskReviewerPairs.Reviewer2Id;
                sub.UpdatedAt = now;
                sub.Version++;
            }

            await SaveSubmissionsAsync();
            await transaction.CommitAsync();
        }

        public async Task UpdateAssignedReviewer(ClaimsPrincipal? claimsPrincipal, Guid Etap1SeriesId, Guid task_item_id, AdminUpdateReviewersDto taskReviewerPairs)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1SeriesId);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot assign reviewers before the answer period ends");
            }

            if (taskReviewerPairs.OldReviewer == taskReviewerPairs.NewReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Invalid old and new reviewer are the same");
            }

            var validReviewers = await _accountModule.Internal_GetReviewerIdsAsync([taskReviewerPairs.NewReviewer]);
            if (validReviewers.Count != 1)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "The new reviewer must be a verified user with the reviewer role");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var submissions = await _db.TaskSubmissions
                .Include(s => s.TaskPart)
                .ThenInclude(tp => tp.TaskItem)
                .Where(s => s.TaskPart.TaskItem.Etap1SeriesId == Etap1SeriesId && s.TaskPart.TaskItemId == task_item_id && ((s.Reviewer1Id == taskReviewerPairs.OldReviewer && s.Review1Score == null) || (s.Reviewer2Id == taskReviewerPairs.OldReviewer && s.Review2Score == null)))
                .OrderBy(s => s.TaskPart.TaskItemId)
                .ThenBy(s => s.TaskPartId)
                .ToListAsync();

            if (!submissions.Any())
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "did not found un asigned tasks that can be given to the reviewer");
            }

            if (submissions.Any(s => s.Reviewer1Id == taskReviewerPairs.NewReviewer || s.Reviewer2Id == taskReviewerPairs.NewReviewer))
                throw new KoalaException(KoalaErrorCodes.Conflict, "The new reviewer is already assigned to an affected submission");

            var now = DateTime.UtcNow;

            foreach (var sub in submissions)
            {
                if (sub.Reviewer1Id == taskReviewerPairs.OldReviewer)
                {
                    sub.Reviewer1Id = taskReviewerPairs.NewReviewer;
                }
                if (sub.Reviewer2Id == taskReviewerPairs.OldReviewer)
                {
                    sub.Reviewer2Id = taskReviewerPairs.NewReviewer;
                }
                sub.UpdatedAt = now;
                sub.Version++;
            }

            await SaveSubmissionsAsync();
            await transaction.CommitAsync();
        }
    }
}
