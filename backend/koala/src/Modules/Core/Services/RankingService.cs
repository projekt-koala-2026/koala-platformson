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
