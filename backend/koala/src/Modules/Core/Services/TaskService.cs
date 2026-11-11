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

        private async Task InvalidateRankingAsync(SubEdition subedition)
        {
            subedition.RankingPublished = false;
            var rankings = await _db.Rankings.Where(r => r.SubeditionId == subedition.Id).ToListAsync();
            _db.Rankings.RemoveRange(rankings);
        }

        private async Task InvalidateAnswersAsync(TaskItem task)
        {
            var submissions = await _db.TaskSubmissions
                .Where(s => s.TaskId == task.Id && s.Review1Score == null && s.Review2Score == null && s.AutoReviewState != null)
                .ToListAsync();
            foreach (var submission in submissions)
            {
                submission.AutoReviewState = null;
                submission.UpdatedAt = DateTime.UtcNow;
                submission.Version++;
            }

            var subedition = await _db.SubEditions.FirstAsync(s => s.Id == task.SubEditionId);
            await InvalidateRankingAsync(subedition);
        }

        public async Task<TaskAnswerDto> CreateTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_item_id, TaskAnswerCreateDto createTaskAnswerDto)
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

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_item_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task item of provided id does not exist");
            }

            var newTaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == createTaskAnswerDto.Answer && tc.TaskItem.Id == task_item_id);

            if (newTaskAnswer != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerAlreadyExists, "Task answer for this task with this answer already exists");
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
            await InvalidateAnswersAsync(task);
            await SaveSubmissionsAsync();

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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            var TaskAnswerWithThisNameExists = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Answer == updateTaskAnswerDto.Answer && tc.TaskItem.Id == task_id && tc.Id != task_answer_id);

            if (TaskAnswerWithThisNameExists != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerAlreadyExists, "task answer for this task with this answer already exists thus canot change this task answer to be the same ! Duplication");
            }

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(tc => tc.Id == task_answer_id && tc.TaskItemId == task.Id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided task_answer_id and task_id does not exist");
            }

            if (TaskAnswer.Version != updateTaskAnswerDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "TaskAnswer versions do not match make sure u have the freshest data");
            }

            TaskAnswer.Answer = updateTaskAnswerDto.Answer;
            TaskAnswer.UpdatedAt = DateTime.UtcNow;
            TaskAnswer.Version++;

            await InvalidateAnswersAsync(task);
            await SaveSubmissionsAsync();

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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var TaskAnswer = await _db.TaskAnswers.FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItemId == task_id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
            }

            _db.TaskAnswers.Remove(TaskAnswer);
            var task = await _db.TaskItems.FirstAsync(t => t.Id == task_id);
            await InvalidateAnswersAsync(task);
            await SaveSubmissionsAsync();
        }

        public async Task<TaskAnswerDto> GetTaskAnswerAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, Guid task_answer_id)
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
                .Include(tc => tc.TaskItem)
                .FirstOrDefaultAsync(e => e.Id == task_answer_id && e.TaskItemId == task_id);

            if (TaskAnswer == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "Task answer of provided id does not exist");
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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
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

        public async Task<TaskSubmissionDto> UpdateTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid subedition_id, Guid task_id, Guid team_id, UpsertTaskSubmissionDto submitTaskSubmissionDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subedition_id);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            if (teamInfo.EditionId != subedition.EditionId)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "The team does not belong to this edition");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (subedition.AnswerOpen > timeNow || timeNow > subedition.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Teams cannot answer tasks outside the answer period");
            }

            var task = await _db.TaskItems.Include(t => t.SubEdition).ThenInclude(s => s.Edition)
                .FirstOrDefaultAsync(t => t.Id == task_id && t.SubEditionId == subedition_id);
            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task does not belong to the specified subedition");
            }
            if (task.SubEdition.State != SubEditionStates.STATE_ACTIVE || task.SubEdition.Edition.State != EditionStates.STATE_ACTIVE ||
                (task.Tier != TaskItemTiers.TIER_1 && task.Tier != TaskItemTiers.TIER_2) ||
                (teamInfo.Tier == TaskItemTiers.TIER_1 && task.Tier != TaskItemTiers.TIER_1) ||
                (teamInfo.Tier != TaskItemTiers.TIER_1 && teamInfo.Tier != TaskItemTiers.TIER_2))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "This task is not available to the team");
            }

            TaskSubmission? submission = null;
            if (teamInfo.Tier == TaskItemTiers.TIER_1)
            {
                submission = await _db.TaskSubmissions.Include(e => e.TaskItem).FirstOrDefaultAsync(e => e.TaskId == task_id && e.TaskItem.Tier == TaskItemTiers.TIER_1 && e.TeamId == team_id);
            }
            if (teamInfo.Tier == TaskItemTiers.TIER_2)
            {
                submission = await _db.TaskSubmissions.Include(e => e.TaskItem).FirstOrDefaultAsync(e => e.TaskId == task_id && (e.TaskItem.Tier == TaskItemTiers.TIER_1 || e.TaskItem.Tier == TaskItemTiers.TIER_2) && e.TeamId == team_id);
            }

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
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "U cant update when having stale data");
            }

            submission.Submission = submitTaskSubmissionDto.Submission;
            submission.UpdatedAt = timeNow;
            submission.Version++;

            await SaveSubmissionsAsync();

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
                submission.State,
                submission.CreatedAt,
                submission.UpdatedAt,
                submission.Version
            );
        }

        public async Task<TaskSubmissionDto> GetTaskSubmissionTeamAsync(ClaimsPrincipal? claimsPrincipal, Guid subedition_id, Guid task_id, Guid team_id)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (claimsPrincipal?.IsInRole("TEAM_CAPTAIN") != true)
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var team = await _accountModule.Internal_GetTeamInfo(team_id);
            if (team == null || team.CaptainId != ClaimsHelper.GetUserGuid(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only the team captain can view this submission");

            var timeNow = DateTime.UtcNow;
            var submission = await _db.TaskSubmissions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TeamId == team_id && s.TaskId == task_id &&
                    s.TaskItem.SubEditionId == subedition_id && s.TaskItem.SubEdition.EditionId == team.EditionId &&
                    s.TaskItem.SubEdition.State == SubEditionStates.STATE_ACTIVE &&
                    s.TaskItem.SubEdition.Edition.State == EditionStates.STATE_ACTIVE &&
                    s.TaskItem.SubEdition.AnswerOpen <= timeNow && s.TaskItem.SubEdition.AnswerClose >= timeNow &&
                    (s.TaskItem.Tier == TaskItemTiers.TIER_1 || s.TaskItem.Tier == TaskItemTiers.TIER_2) &&
                    (team.Tier == TaskItemTiers.TIER_2 || (team.Tier == TaskItemTiers.TIER_1 && s.TaskItem.Tier == TaskItemTiers.TIER_1)));
            if (submission == null)
                throw new KoalaException(KoalaErrorCodes.TaskSubmissionNotFound, "Submission for this team and task does not exist");

            return new TaskSubmissionDto(submission.TeamId, submission.TaskId, null, null, null, null, null,
                submission.Submission, submission.State, submission.CreatedAt, submission.UpdatedAt, submission.Version);
        }

        public async Task<(List<TaskSubmissionDto>, ApiPagination)> GetTaskSubmissionsAsync(ClaimsPrincipal? claimsPrincipal, Guid task_id, PageQueryDto pageQueryDto)
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
        public async Task<TaskSubmissionDto> UpsertScoreByReviewerAsync(ClaimsPrincipal? claimsPrincipal, Guid reviewer_id, Guid task_id, Guid team_id, Guid subedition_id, UpsertReviewDto upsertReviewDto)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subedition_id);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= subedition.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot score submissions before the answer period ends");
            }

            var submission = await _db.TaskSubmissions.FirstOrDefaultAsync(e => e.TaskId == task_id && e.TeamId == team_id && e.TaskItem.SubEditionId == subedition_id);

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
            if (previousScore != upsertReviewDto.Score)
                await InvalidateRankingAsync(subedition);
            submission.UpdatedAt = DateTime.UtcNow;
            submission.Version++;

            await SaveSubmissionsAsync();

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

        public async Task<AutoCheckProgressDto> GetAutoCheckProgressAsync(ClaimsPrincipal? claimsPrincipal, Guid subeditionId)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can view automatic checking progress");
            if (!await _db.SubEditions.AnyAsync(s => s.Id == subeditionId))
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");

            return await _db.TaskSubmissions.AsNoTracking()
                .Where(s => s.TaskItem.SubEditionId == subeditionId)
                .GroupBy(s => 1)
                .Select(g => new AutoCheckProgressDto(
                    g.Count(),
                    g.Count(s => s.AutoReviewState == null && s.Review1Score == null && s.Review2Score == null && s.TaskItem.TaskAnswers.Any()),
                    g.Count(s => s.AutoReviewState == null && s.Review1Score == null && s.Review2Score == null && !s.TaskItem.TaskAnswers.Any()),
                    g.Count(s => s.AutoReviewState == null && (s.Review1Score != null || s.Review2Score != null))))
                .FirstOrDefaultAsync() ?? new AutoCheckProgressDto(0, 0, 0, 0);
        }

        public async Task AutoCheckSubmissionsAsync(ClaimsPrincipal? claimsPrincipal, Guid subedition_id)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subedition_id);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            if (DateTime.UtcNow <= subedition.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot check submissions before the answer period ends");
            }

            // 1. Pobierz zadania dla danej podedycji wraz z ich poprawnymi odpowiedziami z tabeli Answers
            var tasksWithAnswers = await _db.TaskItems
                .Where(t => t.SubEditionId == subedition_id)
                .Join(_db.TaskAnswers,
                      task => task.Id,
                      answer => answer.TaskItemId,
                      (task, answer) => new { Task = task, Answer = answer })
                .ToListAsync();

            if (!tasksWithAnswers.Any())
            {
                throw new KoalaException(KoalaErrorCodes.TaskAnswerNotFound, "No task answers to iniciate auto check");
            }

            var taskIds = tasksWithAnswers.Select(x => x.Task.Id).ToList();

            var submissions = await _db.TaskSubmissions
                .Where(ts => taskIds.Contains(ts.TaskId) && ts.Review1Score == null && ts.Review2Score == null)
                .ToListAsync();

            var correctAnswerMap = tasksWithAnswers.GroupBy(x => x.Task.Id)
                .ToDictionary(group => group.Key, group => group.Select(x => x.Answer.Answer).ToList());

            bool hasChanges = false;
            foreach (var submission in submissions)
            {
                if (correctAnswerMap.TryGetValue(submission.TaskId, out var correctAnswers))
                {
                    bool isCorrect = correctAnswers.Any(correctAnswer => string.Equals(
                        submission.Submission?.Trim(), correctAnswer?.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (submission.AutoReviewState != isCorrect)
                    {
                        submission.AutoReviewState = isCorrect;
                        submission.UpdatedAt = DateTime.UtcNow;
                        submission.Version++;
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges) await InvalidateRankingAsync(subedition);
            await SaveSubmissionsAsync();
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

            var query = _db.TaskSubmissions.Where(t => (t.Reviewer1Id == reviewer_id || t.Reviewer2Id == reviewer_id) && t.Review1Score != t.Review2Score && t.Review1Score != null && t.Review2Score != null).AsNoTracking();

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

        private record ReviewGroup(Guid? Reviewer1Id, Guid? Reviewer2Id, int Total, int Reviewer1Reviewed, int Reviewer2Reviewed, int PendingSubmissions);

        private async Task<List<ReviewGroup>> GetReviewGroupsAsync(ClaimsPrincipal? claimsPrincipal, Guid taskId)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can view review progress");
            if (!await _db.TaskItems.AnyAsync(t => t.Id == taskId))
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");

            return await _db.TaskSubmissions.AsNoTracking().Where(s => s.TaskId == taskId)
                .GroupBy(s => new { s.Reviewer1Id, s.Reviewer2Id })
                .Select(g => new ReviewGroup(g.Key.Reviewer1Id, g.Key.Reviewer2Id, g.Count(),
                    g.Count(s => s.Review1Score != null), g.Count(s => s.Review2Score != null),
                    g.Count(s => s.Review1Score == null || s.Review2Score == null)))
                .ToListAsync();
        }

        public async Task<TaskReviewProgressDto> GetReviewProgressAsync(ClaimsPrincipal? claimsPrincipal, Guid taskId)
        {
            var groups = await GetReviewGroupsAsync(claimsPrincipal, taskId);
            return new TaskReviewProgressDto(groups.Sum(g => g.Total),
                groups.Where(g => g.Reviewer1Id == null && g.Reviewer2Id == null).Sum(g => g.Total),
                groups.Where(g => (g.Reviewer1Id == null) != (g.Reviewer2Id == null)).Sum(g => g.Total),
                groups.Where(g => g.Reviewer1Id != null && g.Reviewer2Id != null)
                    .Select(g => new ReviewPairProgressDto(g.Reviewer1Id!.Value, g.Reviewer2Id!.Value, g.Total,
                        g.Reviewer1Reviewed, g.Reviewer2Reviewed, g.Total - g.Reviewer1Reviewed,
                        g.Total - g.Reviewer2Reviewed, g.PendingSubmissions)).ToList());
        }

        public async Task<List<PendingReviewerDto>> GetPendingReviewersAsync(ClaimsPrincipal? claimsPrincipal, Guid taskId)
        {
            var groups = await GetReviewGroupsAsync(claimsPrincipal, taskId);
            var slots = groups.SelectMany(g => new[] {
                (Reviewer: g.Reviewer1Id, Other: g.Reviewer2Id, Pending: g.Total - g.Reviewer1Reviewed),
                (Reviewer: g.Reviewer2Id, Other: g.Reviewer1Id, Pending: g.Total - g.Reviewer2Reviewed)
            }).Where(s => s.Reviewer != null && s.Pending > 0);
            return slots.GroupBy(s => s.Reviewer!.Value).Select(g => new PendingReviewerDto(g.Key,
                g.Sum(s => s.Pending), g.Where(s => s.Other != null).Select(s => s.Other!.Value).Distinct().ToList())).ToList();
        }

        public async Task AssignReviewersAsync(ClaimsPrincipal? claimsPrincipal, Guid subEditionId, Guid taskId, AssignReviewersDto taskReviewerPairs)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subEditionId);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= subedition.AnswerClose)
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
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM core.tasks WHERE id = {taskId} FOR UPDATE");

            var hasDifferentAssignments = await _db.TaskSubmissions.AnyAsync(s =>
                s.TaskId == taskId && s.TaskItem.SubEditionId == subEditionId &&
                (s.Reviewer1Id != null || s.Reviewer2Id != null) &&
                !((s.Reviewer1Id == taskReviewerPairs.Reviewer1Id && s.Reviewer2Id == taskReviewerPairs.Reviewer2Id) ||
                  (s.Reviewer1Id == taskReviewerPairs.Reviewer2Id && s.Reviewer2Id == taskReviewerPairs.Reviewer1Id)));
            if (hasDifferentAssignments)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Existing submissions have a different or incomplete reviewer assignment");

            var submissions = await _db.TaskSubmissions
                .Include(s => s.TaskItem)
                .Where(s => s.TaskItem.SubEditionId == subEditionId && s.TaskId == taskId && s.Reviewer1Id == null && s.Reviewer2Id == null)
                .OrderBy(s => s.TaskId)
                .ThenBy(s => s.TeamId)
                .ToListAsync();

            if (!submissions.Any())
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "did not found un asigned tasks that can be given to the reviewer");
            }

            var now = DateTime.UtcNow;

            foreach (var sub in submissions)
            {
                sub.Reviewer1Id = taskReviewerPairs.Reviewer1Id;
                sub.Reviewer2Id = taskReviewerPairs.Reviewer2Id;
                sub.UpdatedAt = now;
                sub.Version++;
            }

            await InvalidateRankingAsync(subedition);
            await SaveSubmissionsAsync();
            await transaction.CommitAsync();
        }

        public async Task UpdateAssignedReviewer(ClaimsPrincipal? claimsPrincipal, Guid subEditionId, Guid taskId, AdminUpdateReviewersDto taskReviewerPairs)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subEditionId);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (timeNow <= subedition.AnswerClose)
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
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM core.tasks WHERE id = {taskId} FOR UPDATE");

            var submissions = await _db.TaskSubmissions
                .Include(s => s.TaskItem)
                .Where(s => s.TaskItem.SubEditionId == subEditionId && s.TaskId == taskId && ((s.Reviewer1Id == taskReviewerPairs.OldReviewer && s.Review1Score == null) || (s.Reviewer2Id == taskReviewerPairs.OldReviewer && s.Review2Score == null)))
                .OrderBy(s => s.TaskId)
                .ThenBy(s => s.TeamId)
                .ToListAsync();

            if (!submissions.Any())
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "did not found un asigned tasks that can be given to the reviewer");
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

            await InvalidateRankingAsync(subedition);
            await SaveSubmissionsAsync();
            await transaction.CommitAsync();
        }
    }
}
