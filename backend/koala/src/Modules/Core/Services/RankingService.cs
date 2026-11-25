using System.Security.Claims;
using koala.src.Modules.Account.Services;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Engines;

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

        public async Task CreateRankingsFromEtap1SeriesAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid Etap1Series_id)
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

            var Etap1Series = await _db.Etap1Series.Include(se => se.Edition).Include(se=> se.Tier1Ranking).Include(se => se.Tier2Ranking).FirstOrDefaultAsync(se => se.Id == Etap1Series_id && se.EditionId == edition_id);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist or is not a part of this edition");
            }
            DateTime timeNow = DateTime.UtcNow;
            if (Etap1Series.State != LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Rankings can only be generated for a closed Etap1Series");
            }
            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.Etap1SeriesId == Etap1Series_id &&
                (ts.Reviewer1Id == null || ts.Reviewer2Id == null || ts.Reviewer1Id == ts.Reviewer2Id ||
                ts.Review1Score == null || ts.Review2Score == null || ts.Review1Score != ts.Review2Score)))
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Not all task submissions have a final score");
            }

            //FIXME: WHEN I UPDATE TASKS MAKE SURE TO COME BACK HERE AND FIX THIS
            var tasks = await _db.TaskItems
                .Where(t => t.Etap1SeriesId == Etap1Series_id)
                .ToListAsync();
            int tier1TaskCount = tasks.Count(t => t.Tier == CompetitionTiers.TIER_1);
            int tier2TaskCount = tasks.Count(t => t.Tier == CompetitionTiers.TIER_2);

            if (tier1TaskCount + tier2TaskCount == 0)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "No tasks have been found for this edition");
            }

            var submissions = await _db.TaskSubmissions
                .Include(ts => ts.TaskItem)
                .Where(ts => ts.TaskItem.Etap1SeriesId == Etap1Series_id)
                .ToListAsync();
            var teams = await _accountModule.Internal_GetTeamsForEditionAsync(Etap1Series.EditionId);
            if (teams.Count == 0)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "No teams are registered for this edition");
            }

            var tier1RankingData = await _db.RankingDatas.Where(rd => rd.RankingId == Etap1Series.Tier1RankingId).ToListAsync();
            var tier2RankingData = await _db.RankingDatas.Where(rd => rd.RankingId == Etap1Series.Tier2RankingId).ToListAsync();
            _db.RankingDatas.RemoveRange(tier1RankingData);
            _db.RankingDatas.RemoveRange(tier2RankingData);  

            await _db.SaveChangesAsync();

            tier1RankingData = new List<RankingData>();
            tier2RankingData = new List<RankingData>();

            
            Etap1Series.Tier1Ranking.Name = Etap1Series.Name + "- szkoły podstawowe";
            Etap1Series.Tier1Ranking.UpdatedAt = timeNow;
            Etap1Series.Tier1Ranking.Version++;

            Etap1Series.Tier2Ranking.Name = Etap1Series.Name + "- szkoły ponad podstawowe";
            Etap1Series.Tier2Ranking.UpdatedAt = timeNow;
            Etap1Series.Tier2Ranking.Version++;

            var teamRankingsData = submissions.ToLookup(ts => ts.TeamId);

            List<RankingData> RankingDataTier1 = new List<RankingData>();
            List<RankingData> RankingDataTier2 = new List<RankingData>();

            foreach (var teamInfo in teams)
            {

                if (teamInfo.Tier != CompetitionTiers.TIER_1 && teamInfo.Tier != CompetitionTiers.TIER_2)
                {
                    throw new KoalaException(KoalaErrorCodes.Conflict, "A team has an invalid tier");
                }
                if (teamInfo.Tier == CompetitionTiers.TIER_1)
                {
                    var rankingData = new RankingData
                    {
                        Id = Guid.CreateVersion7(),
                        RankingId = Etap1Series.Tier1Ranking.RankingId,
                        TeamName = teamInfo.Name,
                        HasQualifiedToNextEtap = false,
                        PointsAchived = teamRankingsData[teamInfo.TeamId].Count(ts => ts.Review1Score == true),
                        PointsMax = tier1TaskCount,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    };
                    RankingDataTier1.Add(rankingData);
                }
                if (teamInfo.Tier == CompetitionTiers.TIER_2)
                {
                    var rankingData = new RankingData
                    {
                        Id = Guid.CreateVersion7(),
                        RankingId = Etap1Series.Tier2Ranking.RankingId,
                        TeamName = teamInfo.Name,
                        HasQualifiedToNextEtap = false,
                        PointsAchived = teamRankingsData[teamInfo.TeamId].Count(ts => ts.Review1Score == true),
                        PointsMax = tier1TaskCount + tier2TaskCount,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    };
                    RankingDataTier2.Add(rankingData);
                }
            }

            await _db.RankingDatas.AddRangeAsync(RankingDataTier1);
            await _db.RankingDatas.AddRangeAsync(RankingDataTier2);

            await _db.SaveChangesAsync();
        }

        public async Task GenereteEditionTotalRanking(ClaimsPrincipal claimsPrincipal, Guid edition_id)
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

            var edition = await _db.Editions
                .Include(e => e.Tier1RankingTotal)
                .Include(e => e.Tier2RankingTotal)

                .Include(e => e.Etap1Series)
                .ThenInclude(e1s => e1s.Tier1Ranking)
                
                .Include(e => e.Etap1Series)
                .ThenInclude(e1s => e1s.Tier2Ranking)
                
                .FirstOrDefaultAsync(e => e.Id == edition_id);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition not found");
            }

            if(!edition.Etap1Series.Any())
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series not found for this edition");
            }

            DateTime timeNow = DateTime.UtcNow;


            var tier1TotalRankingData = await _db.RankingDatas.Where(rd => rd.RankingId == edition.Tier1RankingTotalId).ToListAsync();
            var tier2TotalRankingData = await _db.RankingDatas.Where(rd => rd.RankingId == edition.Tier2RankingTotalId).ToListAsync();
            _db.RankingDatas.RemoveRange(tier1TotalRankingData);
            _db.RankingDatas.RemoveRange(tier2TotalRankingData);  

            await _db.SaveChangesAsync();

            tier1TotalRankingData = new List<RankingData>();
            tier2TotalRankingData = new List<RankingData>();         

            foreach(var series in edition.Etap1Series)
            {
                var seriesTier1RankingData = await _db.RankingDatas.Where(rd => rd.RankingId == series.Tier1RankingId).ToListAsync();
                var seriesTier2RankingData = await _db.RankingDatas.Where(rd => rd.RankingId == series.Tier2RankingId).ToListAsync();

                foreach(var rd in seriesTier1RankingData)
                {
                    var existing = tier1TotalRankingData.FirstOrDefault(trd => trd.TeamName == rd.TeamName);

                    if (existing != null)
                    {
                        existing.PointsAchived = existing.PointsAchived + rd.PointsAchived;
                        existing.PointsMax = existing.PointsMax + rd.PointsMax;
                    }
                    else
                    {
                        var rankingData = new RankingData
                        {
                            Id = Guid.CreateVersion7(),
                            RankingId = edition.Tier1RankingTotalId,
                            TeamName = rd.TeamName,
                            HasQualifiedToNextEtap = false,
                            PointsAchived = rd.PointsAchived,
                            PointsMax = rd.PointsMax,
                            CreatedAt = timeNow,
                            UpdatedAt = timeNow,
                            Version = 1
                        };
                        tier1TotalRankingData.Add(rankingData);
                    }
                }

                foreach(var rd in seriesTier2RankingData)
                {
                    var existing = tier2TotalRankingData.FirstOrDefault(trd => trd.TeamName == rd.TeamName);

                    if (existing != null)
                    {
                        existing.PointsAchived = existing.PointsAchived + rd.PointsAchived;
                        existing.PointsMax = existing.PointsMax + rd.PointsMax;
                    }
                    else
                    {
                        var rankingData = new RankingData
                        {
                            Id = Guid.CreateVersion7(),
                            RankingId = edition.Tier2RankingTotalId,
                            TeamName = rd.TeamName,
                            HasQualifiedToNextEtap = false,
                            PointsAchived = rd.PointsAchived,
                            PointsMax = rd.PointsMax,
                            CreatedAt = timeNow,
                            UpdatedAt = timeNow,
                            Version = 1
                        };
                        tier2TotalRankingData.Add(rankingData);
                    }
                }
            }

            await _db.RankingDatas.AddRangeAsync(tier1TotalRankingData);
            await _db.RankingDatas.AddRangeAsync(tier2TotalRankingData);

            await _db.SaveChangesAsync();
        }

        public async Task<RankingDataDto> UpsertRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid ranking_id, Guid ranking_data_id, RankingDataUpsertDto requestDto)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            DateTime timeNow = DateTime.UtcNow;
            var rankingData = await _db.RankingDatas.FirstOrDefaultAsync(rd => rd.RankingId == ranking_id && rd.Id == ranking_data_id);
            if (rankingData != null)
            {
                if (requestDto.Version == null)
                {
                    throw new KoalaException(KoalaErrorCodes.BadRequest, "Version field is missing or is null");
                }
                if (rankingData.Version != requestDto.Version) throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The ranking row changed; refresh before saving.");
                rankingData.TeamName = requestDto.TeamName;
                rankingData.HasQualifiedToNextEtap = requestDto.HasQualifiedToNextEtap;
                rankingData.PointsAchived = requestDto.PointsAchived;
                rankingData.PointsMax = requestDto.PointsMax;
                rankingData.UpdatedAt = timeNow;
                rankingData.Version++;

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource changed while trying to update");
                }
            }
            else
            {
                rankingData = new RankingData
                {
                    Id = Guid.CreateVersion7(),
                    RankingId = ranking_id,
                    TeamName = requestDto.TeamName,
                    HasQualifiedToNextEtap = requestDto.HasQualifiedToNextEtap,
                    PointsAchived = requestDto.PointsAchived,
                    PointsMax = requestDto.PointsMax,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
                await _db.RankingDatas.AddAsync(rankingData);
                await _db.SaveChangesAsync();
            }

            return new RankingDataDto
            (
                rankingData.Id,
                rankingData.RankingId,
                rankingData.TeamName,
                rankingData.HasQualifiedToNextEtap,
                rankingData.PointsAchived,
                rankingData.PointsMax,
                rankingData.CreatedAt,
                rankingData.UpdatedAt,
                rankingData.Version
            );
        }

        public async Task DeleteRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid ranking_id, Guid ranking_data_id)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            DateTime timeNow = DateTime.UtcNow;
            var rankingData = await _db.RankingDatas.FirstOrDefaultAsync(rd => rd.RankingId == ranking_id && rd.Id == ranking_data_id);

            if(rankingData == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingDataNotFound, "Could not found this ranking data");
            }

            _db.RankingDatas.Remove(rankingData);
            await _db.SaveChangesAsync();
        }
        public async Task MakeTotalRankingPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id);
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition not found");
            }

            edition.AreRankingsPublic = true;
            await _db.SaveChangesAsync();
        }

        public async Task MakeTotalRankingNotPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id);
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition not found");
            }

            edition.AreRankingsPublic = false;
            await _db.SaveChangesAsync();
        }

        public async Task MakeSeriesRankingPublicAsync(ClaimsPrincipal claimsPrincipal, Guid etap1series_id)
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

            var series = await _db.Etap1Series.FirstOrDefaultAsync(e1s => e1s.Id == etap1series_id);
            if (series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series not found");
            }

            series.AreRankingsPublic = true;
            await _db.SaveChangesAsync();
        }

        public async Task MakeSeriesRankingNotPublicAsync(ClaimsPrincipal claimsPrincipal, Guid etap1series_id)
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

            var series = await _db.Etap1Series.FirstOrDefaultAsync(e1s => e1s.Id == etap1series_id);
            if (series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series not found");
            }

            series.AreRankingsPublic = false;
            await _db.SaveChangesAsync();
        }

        public async Task<List<RankingDto>> GetEditionRankingsAsync(ClaimsPrincipal claimsPrincipal, Guid editionId)
        {
            var edition = await _db.Editions
                .Include(e => e.Tier1RankingTotal)
                .Include(e => e.Tier2RankingTotal)
                .Include(e => e.Etap1Series)
                .ThenInclude(e1s => e1s.Tier1Ranking)
                .Include(e => e.Etap1Series)
                .ThenInclude(e1s => e1s.Tier2Ranking)
                .FirstOrDefaultAsync(e => e.Id == editionId);

            if(edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition was not found");
            }

            List<RankingDto> rankingDtos = new List<RankingDto>();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            if(isOrganizationAdmin || edition.AreRankingsPublic == true)
            {
                var tier1Total = new RankingDto
                (
                    edition.Tier1RankingTotal.RankingId,
                    edition.Id,
                    null,
                    edition.AreRankingsPublic,
                    edition.Tier1RankingTotal.Name,
                    CompetitionTiers.TIER_1,
                    edition.Tier1RankingTotal.CreatedAt,
                    edition.Tier1RankingTotal.UpdatedAt,
                    edition.Tier1RankingTotal.Version
                );
                var tier2Total = new RankingDto
                (
                    edition.Tier2RankingTotal.RankingId,
                    edition.Id,
                    null,
                    edition.AreRankingsPublic,
                    edition.Tier2RankingTotal.Name,
                    CompetitionTiers.TIER_2,
                    edition.Tier2RankingTotal.CreatedAt,
                    edition.Tier2RankingTotal.UpdatedAt,
                    edition.Tier2RankingTotal.Version
                );
                rankingDtos.AddRange([tier1Total, tier2Total]);
            }

            foreach(var series in edition.Etap1Series)
            {
                if(isOrganizationAdmin || series.AreRankingsPublic == true)
                {
                    var tier1 = new RankingDto
                    (
                        series.Tier1Ranking.RankingId,
                        null,
                        series.Id,
                        series.AreRankingsPublic,
                        series.Tier1Ranking.Name,
                        CompetitionTiers.TIER_1,
                        series.Tier1Ranking.CreatedAt,
                        series.Tier1Ranking.UpdatedAt,
                        series.Tier1Ranking.Version
                    );
                    var tier2 = new RankingDto
                    (
                        series.Tier2Ranking.RankingId,
                        null,
                        series.Id,
                        series.AreRankingsPublic,
                        series.Tier2Ranking.Name,
                        CompetitionTiers.TIER_2,
                        series.Tier2Ranking.CreatedAt,
                        series.Tier2Ranking.UpdatedAt,
                        series.Tier2Ranking.Version
                    );
                    rankingDtos.AddRange([tier1, tier2]);
                }
            }

            return rankingDtos;
        }

        public async Task<(List<RankingDataDto>, ApiPagination)> GetEtap1SeriesRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid rankingId, Guid etap1series_id, PageQueryDto queryDto)
        {
            var query = _db.Etap1Series.AsNoTracking().AsQueryable();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if(!isOrganizationAdmin)
            {
                query = query.Where(s => s.AreRankingsPublic);
            }

            var series = await query.FirstOrDefaultAsync(s => s.Id == etap1series_id &&
                (s.Tier1RankingId == rankingId || s.Tier2RankingId == rankingId));
            if (series == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                return (await _db.RankingDatas.Where(r => r.RankingId == rankingId && r.PointsAchived>0)
                .OrderByDescending(rd => rd.PointsAchived).ThenBy(rd => rd.TeamName).ThenBy(rd => rd.Id)
                .Skip(queryDto.PageSize * queryDto.PageNumber)
                .Take(queryDto.PageSize)
                .Select(rd => new RankingDataDto
                (
                    rd.Id,
                    rd.RankingId,
                    rd.TeamName,
                    rd.HasQualifiedToNextEtap,
                    rd.PointsAchived,
                    rd.PointsMax,
                    rd.CreatedAt,
                    rd.UpdatedAt,
                    rd.Version
                )).ToListAsync(),
                new ApiPagination(queryDto.PageNumber, queryDto.PageSize, await _db.RankingDatas.CountAsync(r => r.RankingId == rankingId && r.PointsAchived>0)));
            }
            {
                return (await _db.RankingDatas.Where(r => r.RankingId == rankingId)
                .OrderByDescending(rd => rd.PointsAchived).ThenBy(rd => rd.TeamName).ThenBy(rd => rd.Id)
                .Skip(queryDto.PageSize * queryDto.PageNumber)
                .Take(queryDto.PageSize)
                .Select(rd => new RankingDataDto
                (
                    rd.Id,
                    rd.RankingId,
                    rd.TeamName,
                    rd.HasQualifiedToNextEtap,
                    rd.PointsAchived,
                    rd.PointsMax,
                    rd.CreatedAt,
                    rd.UpdatedAt,
                    rd.Version
                )).ToListAsync(),
                new ApiPagination(queryDto.PageNumber, queryDto.PageSize, await _db.RankingDatas.CountAsync(r => r.RankingId == rankingId)));   
            }
        }

        public async Task<(List<RankingDataDto>, ApiPagination)> GetEditionRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid rankingId, Guid edition_id, PageQueryDto queryDto)
        {
            var query = _db.Editions.AsNoTracking().AsQueryable();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if(!isOrganizationAdmin)
            {
                query = query.Where(e => e.AreRankingsPublic);
            }

            var edition = await query.FirstOrDefaultAsync(e => e.Id == edition_id &&
                (e.Tier1RankingTotalId == rankingId || e.Tier2RankingTotalId == rankingId));
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);

            if (!isAuthenticated)
            {
                return (await _db.RankingDatas.Where(r => r.RankingId == rankingId && r.PointsAchived>0)
                .OrderByDescending(rd => rd.PointsAchived).ThenBy(rd => rd.TeamName).ThenBy(rd => rd.Id)
                .Skip(queryDto.PageSize * queryDto.PageNumber)
                .Take(queryDto.PageSize)
                .Select(rd => new RankingDataDto
                (
                    rd.Id,
                    rd.RankingId,
                    rd.TeamName,
                    rd.HasQualifiedToNextEtap,
                    rd.PointsAchived,
                    rd.PointsMax,
                    rd.CreatedAt,
                    rd.UpdatedAt,
                    rd.Version
                )).ToListAsync(),
                new ApiPagination(queryDto.PageNumber, queryDto.PageSize, await _db.RankingDatas.CountAsync(r => r.RankingId == rankingId && r.PointsAchived>0)));
            }
            {
                return (await _db.RankingDatas.Where(r => r.RankingId == rankingId)
                .OrderByDescending(rd => rd.PointsAchived).ThenBy(rd => rd.TeamName).ThenBy(rd => rd.Id)
                .Skip(queryDto.PageSize * queryDto.PageNumber)
                .Take(queryDto.PageSize)
                .Select(rd => new RankingDataDto
                (
                    rd.Id,
                    rd.RankingId,
                    rd.TeamName,
                    rd.HasQualifiedToNextEtap,
                    rd.PointsAchived,
                    rd.PointsMax,
                    rd.CreatedAt,
                    rd.UpdatedAt,
                    rd.Version
                )).ToListAsync(),
                new ApiPagination(queryDto.PageNumber, queryDto.PageSize, await _db.RankingDatas.CountAsync(r => r.RankingId == rankingId)));   
            }
        }


    }
}
