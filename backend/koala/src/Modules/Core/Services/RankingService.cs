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
    public class RankingService
    {
        private readonly CoreDbContext _db;
        private readonly IAccountModule _accountModule;

        public RankingService(CoreDbContext db, IAccountModule accountModule)
        {
            _db = db;
            _accountModule = accountModule;
        }        

        public async Task GenereteRankingFromSubEdtionAsync(ClaimsPrincipal claimsPrincipal, Guid subEditionId)
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

            var subedition = await _db.SubEditions.FirstOrDefaultAsync(se => se.Id == subEditionId);
            if(subedition == null)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if(subedition.AnswerOpen <= timeNow && timeNow <= subedition.AnswerClose)
            {
                throw new CoreException(CoreErrorCodes.SubEditionNotFound, "Teams have still time to answer tasks");
            }

            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.SubEditionId == subEditionId && ((ts.Review1Score != ts.Review2Score) || ts.Review1Score == null || ts.Review2Score == null)))
            {
                throw new CoreException(CoreErrorCodes.TaskSubmissionNotFound, "Not all task submissions are yet scored");
            }
            var tasks = await _db.TaskItems
                .Where(t => t.SubEditionId == subEditionId)
                .ToListAsync();
        
            int tier1TaskCount = tasks.Count(t => t.Tier == TaskItemTiers.TIER_1);
            int tier2TaskCount = tasks.Count(t => t.Tier == TaskItemTiers.TIER_2);
        
            // 2. Pobierz zgłoszenia wraz z powiązanymi zadaniami
            var submissions = await _db.TaskSubmissions
                .Include(ts => ts.TaskItem)
                .Where(ts => ts.TaskItem.SubEditionId == subEditionId)
                .ToListAsync();
        
            // 3. Grupowanie po drużynach i sumowanie punktów
            var teamRankingsData = submissions
                .GroupBy(ts => ts.TeamId)
                .Select(group => new
                {
                    TeamId = group.Key,
                    TotalScore = group.Sum(ts => ts.Review1Score == true ? 1 : 0)
                })
                .OrderByDescending(x => x.TotalScore)
                .ToList();
        
            List<Ranking> newRankings = new List<Ranking>();
        
            foreach (var data in teamRankingsData)
            {
                var teamInfo = await _accountModule.Internal_GetTeamInfo(data.TeamId);
        
                var ranking = new Ranking
                {
                    Id = Guid.CreateVersion7(),
                    SubeditionId = subEditionId,
                    TeamName = teamInfo.Name, 
                    Tier = teamInfo.Tier,
                    Points = data.TotalScore,
                    PointsMax = teamInfo.Tier == TaskItemTiers.TIER_2 ? tier1TaskCount + tier2TaskCount : tier1TaskCount
                };
        
                newRankings.Add(ranking);
            }

            _db.Rankings.AddRange(newRankings);
            await _db.SaveChangesAsync();
        }

        //TODO: FINISH THEM LATER
        // public async Task CreateRankingForSubEdtionAsync(ClaimsPrincipal claimsPrincipal, Guid subEditionId)
        // {
            
        // }

        // public async Task UpdateRankingForSubEdtionAsync(ClaimsPrincipal claimsPrincipal, Guid subEditionId)
        // {
            
        // }

        public async Task<(List<RankingDto>, ApiPagination)> GetRankingAsync(string? tier, PageQueryDto pageQueryDto)
        {
            var validTiers = new[] { TaskItemTiers.TIER_1, TaskItemTiers.TIER_2 };
            var selectedTier = !string.IsNullOrWhiteSpace(tier) && validTiers.Contains(tier)
                ? tier
                : TaskItemTiers.TIER_1;

            var maxScore = await _db.TaskItems
                .AsNoTracking()
                .CountAsync(t => t.Tier == selectedTier);

            var baseQuery = _db.TaskSubmissions
                .AsNoTracking()
                .Where(s => s.TaskItem.Tier == selectedTier);

            var groupedQuery = baseQuery
                .GroupBy(s => s.TeamId)
                .Select(g => new
                {
                    TeamId = g.Key,
                    TotalScore = g.Where(s => s.AutoReviewState == true)
                                .Select(s => s.TaskId)
                                .Distinct()
                                .Count(),
                    LatestUpdatedAt = g.Max(s => (DateTime?)s.UpdatedAt)
                });

            var totalCount = await groupedQuery.CountAsync();
            var skipCount = pageQueryDto.PageSize * pageQueryDto.PageNumber;

            var pagedTeams = await groupedQuery
                .OrderByDescending(r => r.TotalScore)
                .ThenBy(r => r.LatestUpdatedAt)
                .Skip(skipCount)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var rankingDtos = pagedTeams
                .Select(r => new RankingDto(
                    r.TeamId,
                    r.TotalScore,
                    maxScore,
                    selectedTier,
                    r.LatestUpdatedAt
                ))
                .ToList();

            return (rankingDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}    
