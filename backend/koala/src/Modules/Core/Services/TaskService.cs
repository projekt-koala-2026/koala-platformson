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

        public async Task<TaskAnswerDto> CreateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, TaskAnswerCreateDto createTaskAnswerDto)
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

            DateTime timeNow = DateTime.UtcNow;
            newTaskAnswer = new TaskAnswer
            {
                Id = Guid.CreateVersion7(),
                TaskItemId = task_item_id,
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
                newTaskAnswer.TaskItemId,
                newTaskAnswer.Answer,
                newTaskAnswer.CreatedAt,
                newTaskAnswer.UpdatedAt,
                newTaskAnswer.Version
            );
        }

        public async Task<TaskAnswerDto> UpdateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_answer_id, TaskAnswerUpdateDto updateTaskAnswerDto)
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

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Id == task_answer_id && tc.TaskItemId == task.Id);

            if (TaskAnswer == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task answer of provided task_answer_id and task_id does not exist");
            }

            if (TaskAnswer.Version != updateTaskAnswerDto.Version)
            {
                throw new CoreException(CoreErrorCodes.Conflict, "TaskAnswer versions do not match make sure u have the freshest data");
            }

            TaskAnswer.Answer = updateTaskAnswerDto.Answer;
            TaskAnswer.UpdatedAt = DateTime.UtcNow;
            TaskAnswer.Version++;

            await _db.SaveChangesAsync();

            return new TaskAnswerDto
            (
                TaskAnswer.Id,
                TaskAnswer.TaskItemId,
                TaskAnswer.Answer,
                TaskAnswer.CreatedAt,
                TaskAnswer.UpdatedAt,
                TaskAnswer.Version
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

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItemId == task_id);

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
                .FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItemId == task_id);

            if (TaskAnswer == null)
            {
                throw new CoreException(CoreErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }

            return new TaskAnswerDto
            (
                TaskAnswer.Id,
                TaskAnswer.TaskItemId,
                TaskAnswer.Answer,
                TaskAnswer.CreatedAt,
                TaskAnswer.UpdatedAt,
                TaskAnswer.Version
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
                .Select(ta => new TaskAnswerDto
                (
                    ta.Id,
                    ta.TaskItemId,
                    ta.Answer,
                    ta.CreatedAt,
                    ta.UpdatedAt,
                    ta.Version
                ))
                .ToListAsync();

            return (entities, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<TaskSubmissionDto> UpdateTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid team_id, UpsertTaskSubmissionDto submitTaskSubmissionDto)
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

            var submission = await _db.TaskSubmissions.FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id);

            DateTime timeNow = DateTime.UtcNow;

            if (submission == null)
            {
                submission = new TaskSubmission
                {
                    TeamId = team_id,
                    TaskId = task_id,
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
                throw new CoreException(CoreErrorCodes.Conflict, "U cant update when having stale data");
            }

            submission.Submission = submitTaskSubmissionDto.Submission;
            submission.UpdatedAt = timeNow;
            submission.Version++;

            await _db.SaveChangesAsync();

            return new TaskSubmissionDto
            (
                submission.TeamId,
                submission.TaskId,
                null,
                null,
                null,
                null,
                null,
                submission.Submission,
                null,
                submission.CreatedAt,
                submission.UpdatedAt,
                submission.Version
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
                .Select(e => new TaskSubmissionDto
                (
                    e.TeamId,
                    e.TaskId,
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

        public async Task<TaskSubmissionDto> AddScoreByReviewerAsync(ClaimsPrincipal claimsPrincipal, Guid reviewer_id, Guid task_id, Guid team_id, AddReviewDto addReviewDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsOrganizationReviewer(claimsPrincipal);
            
            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have reviewer role");
            }


            var submission = await _db.TaskSubmissions
                .FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id);

            if (submission == null)
            {
                throw new CoreException(CoreErrorCodes.TaskSubmissionNotFound, "Task submission does not exist");
            }

            if (submission.Reviewer1Id == reviewer_id)
            {
                submission.Review1Score = addReviewDto.Score;
                if (submission.Review1Score == submission.Review2Score && submission.Review2Score != null)
                {
                    submission.AutoReviewState = submission.Review1Score;
                }

            }
            else if (submission.Reviewer2Id == reviewer_id)
            {
                submission.Review2Score = addReviewDto.Score;
                if (submission.Review1Score == submission.Review2Score && submission.Review1Score != null)
                {
                    submission.AutoReviewState = submission.Review2Score;
                }
            }
            else
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "Reviewer is not assigned to this submission");
            }

            submission.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new TaskSubmissionDto
            (
                submission.TeamId,
                submission.TaskId,
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
        public async Task<TaskSubmissionDto> UpsertScoreByReviewerAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, Guid task_id, Guid team_id, UpsertReviewDto upsertReviewDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var submission = await _db.TaskSubmissions.FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id);

            if (submission == null)
            {
                throw new CoreException(CoreErrorCodes.TaskSubmissionNotFound, "Task submission of provided submission_id does not exist");
            }

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
                throw new CoreException(CoreErrorCodes.Forbidden, "Reviewer is not assigned to this submission");
            }

            submission.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new TaskSubmissionDto
            (
                submission.TeamId,
                submission.TaskId,
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
            bool isOrganizationReviewer = ClaimsHelper.IsOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var query = _db.TaskSubmissions.Where(t => t.Reviewer1Id == reviewer_id || t.Reviewer2Id == reviewer_id).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TeamId)
                .ThenBy(e => e.TaskId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskSubmissions = entities.Select(e =>
            {
                return new TaskSubmissionDto(
                    e.TeamId,
                    e.TaskId,
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

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTasksWithConflictsAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationReviewer = ClaimsHelper.IsOrganizationReviewer(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new CoreException(CoreErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationReviewer)
            {
                throw new CoreException(CoreErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var query = _db.TaskSubmissions.Where(t =>(t.Reviewer1Id == reviewer_id || t.Reviewer2Id == reviewer_id) && t.Review1Score != t.Review2Score && t.Review1Score != null && t.Review2Score != null).AsNoTracking();

            var totalCount = await query.CountAsync();

            var entities = await query
                .OrderBy(e => e.TeamId)
                .ThenBy(e => e.TaskId)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskSubmissions = entities.Select(e =>
            {
                return new TaskSubmissionDto(
                    e.TeamId,
                    e.TaskId,
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

        public async Task<List<TaskSubmissionDto>> AssignReviewersAsync(ClaimsPrincipal? claimsPrincipal, Guid subEditionId, Dictionary<Guid, List<ReviewerPairDto>> taskReviewerPairs)
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

            if (taskReviewerPairs == null || !taskReviewerPairs.Any())
            {
                throw new CoreException(CoreErrorCodes.BadRequest, "No reviewer pairs provided");
            }

            var mappedPairs = taskReviewerPairs.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?
                    .Where(pair => pair.Reviewer1Id != pair.Reviewer2Id)
                    .Select(pair => (pair.Reviewer1Id, pair.Reviewer2Id))
                    .Distinct()
                    .ToList() ?? new List<(Guid Reviewer1Id, Guid Reviewer2Id)>()
            );

            var submissions = await _db.TaskSubmissions
                .Include(s => s.TaskItem)
                .Where(s => s.TaskItem.SubEditionId == subEditionId)
                .OrderBy(s => s.TaskId)
                .ThenBy(s => s.TeamId)
                .ToListAsync();

            if (!submissions.Any())
            {
                return new List<TaskSubmissionDto>();
            }

            var now = DateTime.UtcNow;
            var submissionsByTask = submissions.GroupBy(s => s.TaskId);

            foreach (var group in submissionsByTask)
            {
                if (!mappedPairs.TryGetValue(group.Key, out var pairs) || !pairs.Any())
                {
                    continue;
                }

                var taskSubmissions = group.ToList();
                int pairsCount = pairs.Count;

                for (int i = 0; i < taskSubmissions.Count; i++)
                {
                    var pair = pairs[i % pairsCount];
                    var submission = taskSubmissions[i];

                    submission.Reviewer1Id = pair.Reviewer1Id;
                    submission.Reviewer2Id = pair.Reviewer2Id;
                    submission.UpdatedAt = now;
                    submission.Version++;
                }
            }

            await _db.SaveChangesAsync();

            return submissions.Select(s => new TaskSubmissionDto(
                s.TeamId,
                s.TaskId,
                s.Reviewer1Id,
                s.Reviewer2Id,
                s.AutoReviewState,
                s.Review1Score,
                s.Review2Score,
                s.Submission,
                s.State,
                s.CreatedAt,
                s.UpdatedAt,
                s.Version
            )).ToList();
        }
    }
}