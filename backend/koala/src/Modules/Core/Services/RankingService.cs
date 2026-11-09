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
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var subedition = await _db.SubEditions.Include(se => se.Edition).FirstOrDefaultAsync(se => se.Id == subEditionId);
            if(subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if(subedition.Edition.State != EditionStates.STATE_CLOSED || timeNow <= subedition.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Rankings can only be generated for a closed edition after the answer period ends");
            }

            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.SubEditionId == subEditionId &&
                (ts.AutoReviewState == null || (ts.Review1Score == null) != (ts.Review2Score == null) ||
                ((ts.Reviewer1Id != null || ts.Reviewer2Id != null) && (ts.Review1Score == null || ts.Review2Score == null)) ||
                (ts.Review1Score != null && ts.Review2Score != null && ts.Review1Score != ts.Review2Score))))
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Not all task submissions have a final score");
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

            var teams = await _accountModule.Internal_GetTeamsForEditionAsync(subedition.EditionId);
            if (teams.Count == 0)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "No teams are registered for this edition");
            }
        
            // 3. Grupowanie po drużynach i sumowanie punktów
            var teamRankingsData = submissions.ToLookup(ts => ts.TeamId);
        
            List<Ranking> newRankings = new List<Ranking>();
        
            foreach (var teamInfo in teams)
            {
                if (teamInfo.Tier != TaskItemTiers.TIER_1 && teamInfo.Tier != TaskItemTiers.TIER_2)
                    throw new KoalaException(KoalaErrorCodes.Conflict, "A team has an invalid tier");
        
                var ranking = new Ranking
                {
                    Id = Guid.CreateVersion7(),
                    SubeditionId = subEditionId,
                    TeamId = teamInfo.TeamId,
                    TeamName = teamInfo.Name, 
                    Tier = teamInfo.Tier,
                    Points = teamRankingsData[teamInfo.TeamId].Count(ts => ts.AutoReviewState == true &&
                        (ts.TaskItem.Tier == TaskItemTiers.TIER_1 || (teamInfo.Tier == TaskItemTiers.TIER_2 && ts.TaskItem.Tier == TaskItemTiers.TIER_2))),
                    PointsMax = teamInfo.Tier == TaskItemTiers.TIER_2 ? tier1TaskCount + tier2TaskCount : tier1TaskCount,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
        
                newRankings.Add(ranking);
            }

            _db.Rankings.RemoveRange(await _db.Rankings.Where(r => r.SubeditionId == subEditionId).ToListAsync());
            subedition.RankingPublished = false;
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

        public async Task PublishRankingAsync(ClaimsPrincipal claimsPrincipal, Guid subeditionId)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only an administrator can publish rankings");
            var subedition = await _db.SubEditions.FirstOrDefaultAsync(s => s.Id == subeditionId);
            if (subedition == null)
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist");
            if (!await _db.Rankings.AnyAsync(r => r.SubeditionId == subeditionId))
                throw new KoalaException(KoalaErrorCodes.Conflict, "Generate the ranking before publishing it");
            subedition.RankingPublished = true;
            await _db.SaveChangesAsync();
        }

        public async Task<(List<RankingDto>, ApiPagination)> GetRankingAsync(Guid subeditionId, string? tier, PageQueryDto pageQueryDto, bool includeUnpublished = false)
        {
            if (!string.IsNullOrWhiteSpace(tier) && tier != TaskItemTiers.TIER_1 && tier != TaskItemTiers.TIER_2)
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Unknown ranking tier");
            var query = _db.Rankings.AsNoTracking().Where(r => r.SubeditionId == subeditionId && (includeUnpublished || r.SubEdition.RankingPublished));
            if (!string.IsNullOrWhiteSpace(tier)) query = query.Where(r => r.Tier == tier);
            var totalCount = await query.CountAsync();
            var rankingDtos = await query.OrderByDescending(r => r.Points).ThenBy(r => r.TeamName).ThenBy(r => r.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize)
                .Select(r => new RankingDto(r.TeamId, r.TeamName, r.SubeditionId, r.Points, r.PointsMax, r.Tier, r.UpdatedAt))
                .ToListAsync();
            return (rankingDtos, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}    
