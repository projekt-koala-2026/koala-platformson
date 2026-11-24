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

        public async Task CreateRankingsFromSubEditionAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid sub_edition_id)
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

            var subedition = await _db.SubEditions.Include(se => se.Edition).FirstOrDefaultAsync(se => se.Id == sub_edition_id && se.EditionId == edition_id);
            if (subedition == null)
            {
                throw new KoalaException(KoalaErrorCodes.SubEditionNotFound, "Provided subedition does not exist or is not a part of this edition");
            }
            DateTime timeNow = DateTime.UtcNow;
            if (subedition.State != LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Rankings can only be generated for a closed subedition");
            }
            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.SubEditionId == sub_edition_id &&
                (ts.Reviewer1Id == null || ts.Reviewer2Id == null || ts.Reviewer1Id == ts.Reviewer2Id ||
                ts.Review1Score == null || ts.Review2Score == null || ts.Review1Score != ts.Review2Score)))
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Not all task submissions have a final score");
            }
            var tasks = await _db.TaskItems
                .Where(t => t.SubEditionId == sub_edition_id)
                .ToListAsync();
            int tier1TaskCount = tasks.Count(t => t.Tier == CompetitionTiers.TIER_1);
            int tier2TaskCount = tasks.Count(t => t.Tier == CompetitionTiers.TIER_2);

            if (tier1TaskCount + tier2TaskCount == 0)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "No tasks have been found for this edition");
            }

            Ranking RankingTier1 = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                EditionId = edition_id,
                Name = subedition.Name + "- szkoły podstawowe",
                IsPublic = false,
                Tier = CompetitionTiers.TIER_1,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };
            Ranking RankingTier2 = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                EditionId = edition_id,
                Name = subedition.Name + "- szkoły ponad podstawowe",
                IsPublic = false,
                Tier = CompetitionTiers.TIER_2,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var submissions = await _db.TaskSubmissions
                .Include(ts => ts.TaskItem)
                .Where(ts => ts.TaskItem.SubEditionId == sub_edition_id)
                .ToListAsync();
            var teams = await _accountModule.Internal_GetTeamsForEditionAsync(subedition.EditionId);
            if (teams.Count == 0)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "No teams are registered for this edition");
            }

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
                        RankingId = RankingTier1.RankingId,
                        TeamName = teamInfo.Name,
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
                        RankingId = RankingTier2.RankingId,
                        TeamName = teamInfo.Name,
                        PointsAchived = teamRankingsData[teamInfo.TeamId].Count(ts => ts.Review1Score == true),
                        PointsMax = tier1TaskCount + tier2TaskCount,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    };
                    RankingDataTier2.Add(rankingData);
                }
            }

            await _db.Rankings.AddAsync(RankingTier1);
            await _db.Rankings.AddAsync(RankingTier2);

            await _db.RankingDatas.AddRangeAsync(RankingDataTier1);
            await _db.RankingDatas.AddRangeAsync(RankingDataTier2);

            await _db.SaveChangesAsync();
        }

        public async Task Merge2RankingsFromEditionAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, RankingMergeRequestDto requestDto)
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

            if (requestDto.Ranking1Id == requestDto.Ranking2Id)
            {
                throw new KoalaException(KoalaErrorCodes.RankingCannotMergeTheSameRanking, "Cannot merge a ranking when the same id was provided twice");
            }

            var ranking1 = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == requestDto.Ranking1Id && r.EditionId == edition_id);
            var ranking2 = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == requestDto.Ranking1Id && r.EditionId == edition_id);

            if (ranking1 == null || ranking2 == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "One of the rankings that u try to merge does not exist");
            }

            if (ranking1.Tier != ranking2.Tier)
            {
                throw new KoalaException(KoalaErrorCodes.RankingsTierConflict, "Ranking tiers do not match");
            }

            if (await _db.Rankings.AnyAsync(r => r.Name == requestDto.MergeRankingName && r.EditionId == edition_id))
            {
                throw new KoalaException(KoalaErrorCodes.AlreadyTaken, "This ranking name is already in use in this edition");
            }

            DateTime timeNow = DateTime.UtcNow;

            Ranking MergeRanking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                EditionId = edition_id,
                Name = requestDto.MergeRankingName,
                IsPublic = false,
                Tier = ranking1.Tier,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var ranking1Data = await _db.RankingDatas.Where(rd => rd.RankingId == requestDto.Ranking1Id).OrderByDescending(rd => rd.TeamName).ToListAsync();
            var ranking2Data = await _db.RankingDatas.Where(rd => rd.RankingId == requestDto.Ranking2Id).OrderByDescending(rd => rd.TeamName).ToListAsync();

            List<RankingData> MergeRankingData = new List<RankingData>();

            foreach (var rd in ranking1Data)
            {
                var rankingData = new RankingData
                {
                    Id = Guid.CreateVersion7(),
                    RankingId = MergeRanking.RankingId,
                    TeamName = rd.TeamName,
                    PointsAchived = rd.PointsAchived,
                    PointsMax = rd.PointsMax,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
                MergeRankingData.Add(rankingData);
            }

            foreach (var rd in ranking2Data)
            {
                var existing = MergeRankingData.FirstOrDefault(mrd => mrd.TeamName == rd.TeamName);

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
                        RankingId = MergeRanking.RankingId,
                        TeamName = rd.TeamName,
                        PointsAchived = rd.PointsAchived,
                        PointsMax = rd.PointsMax,
                        CreatedAt = timeNow,
                        UpdatedAt = timeNow,
                        Version = 1
                    };
                    MergeRankingData.Add(rankingData);
                }
            }

            await _db.RankingDatas.AddRangeAsync(MergeRankingData);
            await _db.Rankings.AddAsync(MergeRanking);

            await _db.SaveChangesAsync();
        }

        public async Task CreateRankingManualyAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, RankingCreateDto requestDto)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.Name == requestDto.RankingName && r.EditionId == edition_id);
            if (ranking != null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingAlreadyExists, "Ranking with this name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            Ranking Ranking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                EditionId = edition_id,
                Name = requestDto.RankingName,
                IsPublic = false,
                Tier = requestDto.RankingTier,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };
            List<RankingData> RankingData = new List<RankingData>();
            RankingData.AddRange(requestDto.RankingData.Select(
                rd => new RankingData
                {
                    Id = Guid.CreateVersion7(),
                    RankingId = Ranking.RankingId,
                    TeamName = rd.TeamName,
                    PointsAchived = rd.PointsAchived,
                    PointsMax = rd.PointsMax,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                }));

            await _db.Rankings.AddAsync(Ranking);
            await _db.RankingDatas.AddRangeAsync(RankingData);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteRankingAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid ranking_id)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id && r.EditionId == edition_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            var rankingData = await _db.RankingDatas.Where(rd => rd.RankingId == ranking_id).ToListAsync();

            _db.Rankings.Remove(ranking);
            _db.RankingDatas.RemoveRange(rankingData);

            await _db.SaveChangesAsync();
        }

        public async Task<RankingDto> UpdateRankingAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid ranking_id, RankingUpdateDto requestDto)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id && r.EditionId == edition_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            if (await _db.Rankings.AnyAsync(r => r.Name == requestDto.Name && r.RankingId != ranking.RankingId))
            {
                throw new KoalaException(KoalaErrorCodes.AlreadyTaken, "The ranking name is already in use");
            }
            DateTime timeNow = DateTime.UtcNow;
            ranking.Name = requestDto.Name;
            ranking.Tier = requestDto.Tier;
            ranking.UpdatedAt = timeNow;
            ranking.Version++;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "The data u trying to update undergo changes pls refresh");
            }

            return new RankingDto
            (
                ranking.RankingId,
                ranking.EditionId,
                ranking.IsPublic,
                ranking.Name,
                ranking.Tier,
                ranking.CreatedAt,
                ranking.UpdatedAt,
                ranking.Version
            );
        }

        public async Task<RankingDataDto> UpsertRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid ranking_id, Guid ranking_data_id, RankingDataUpsertDto requestDto)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id && r.EditionId == edition_id);
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
                rankingData.TeamName = requestDto.TeamName;
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
                rankingData.PointsAchived,
                rankingData.PointsMax,
                rankingData.CreatedAt,
                rankingData.UpdatedAt,
                rankingData.Version
            );
        }

        public async Task MakeRankingPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid ranking_id)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id && r.EditionId == edition_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            ranking.IsPublic = true;
            await _db.SaveChangesAsync();
        }

        public async Task MakeRankingNotPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id, Guid ranking_id)
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

            var ranking = await _db.Rankings.FirstOrDefaultAsync(r => r.RankingId == ranking_id && r.EditionId == edition_id);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            ranking.IsPublic = false;
            await _db.SaveChangesAsync();
        }

        public async Task<List<RankingDto>> GetEditionRankingsAsync(ClaimsPrincipal claimsPrincipal, Guid editionId)
        {
            var query = _db.Rankings.AsNoTracking().AsQueryable();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                query = query.Where(r => r.IsPublic == true);
            }

            return await query.Where(r => r.EditionId == editionId)
            .Select(r => new RankingDto
            (
                r.RankingId,
                r.EditionId,
                r.IsPublic,
                r.Name,
                r.Tier,
                r.CreatedAt,
                r.UpdatedAt,
                r.Version
            )).ToListAsync();
        }

        public async Task<(List<RankingDataDto>, ApiPagination)> GetEditionRankingDataAsync(ClaimsPrincipal claimsPrincipal, Guid editionId, Guid rankingId, PageQueryDto queryDto)
        {
            var query = _db.Rankings.AsNoTracking().AsQueryable();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);

            if (!isOrganizationAdmin)
            {
                query = query.Where(r => r.IsPublic == true);
            }

            var ranking = await query.FirstOrDefaultAsync(r => r.RankingId == rankingId && r.EditionId == editionId);
            if (ranking == null)
            {
                throw new KoalaException(KoalaErrorCodes.RankingNotFound, "Ranking not found");
            }

            return (await _db.RankingDatas.Where(r => r.RankingId == ranking.RankingId)
            .OrderBy(rd => rd.PointsAchived)
            .Skip(queryDto.PageSize * queryDto.PageNumber)
            .Take(queryDto.PageSize)
            .Select(rd => new RankingDataDto
            (
                rd.Id,
                rd.RankingId,
                rd.TeamName,
                rd.PointsAchived,
                rd.PointsMax,
                rd.CreatedAt,
                rd.UpdatedAt,
                rd.Version
            )).ToListAsync(),
            new ApiPagination(queryDto.PageNumber, queryDto.PageSize, await _db.RankingDatas.CountAsync(r => r.RankingId == ranking.RankingId)));
        }

        public async Task<List<WinnerDto>> GetEditionWinners(ClaimsPrincipal claimsPrincipal, Guid edition_id)
        {
            if (!await _db.Editions.AnyAsync(e => e.Id == edition_id))
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Provided edition does not exist");
            }

            var query = _db.Editions.AsNoTracking().AsQueryable().Include(e => e.WinnerTier1).Include(e => e.WinnerTier2).Where(e => e.Id == edition_id);

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            if (!isOrganizationAdmin)
            {
                query = query.Where(e => e.AreWinnersPublic == true);
            }

            var edition = await query.FirstOrDefaultAsync();
            if (edition == null)
            {
                return [];
            }
            var winners = new List<WinnerDto>();
            
            if(edition.WinnerTier1!= null && edition.WinnerTier1Id != null)
            {
                
                winners.Add(new WinnerDto
                (
                    edition.WinnerTier1.Id,
                    edition.Id,
                    edition.WinnerTier1.TeamName,
                    CompetitionTiers.TIER_1,
                    edition.WinnerTier1.CreatedAt,
                    edition.WinnerTier1.UpdatedAt,
                    edition.WinnerTier1.Version
                ));
            }
            if(edition.WinnerTier2!= null && edition.WinnerTier2Id != null){
                winners.Add(new WinnerDto
                (
                    edition.WinnerTier2.Id,
                    edition.Id,
                    edition.WinnerTier2.TeamName,
                    CompetitionTiers.TIER_2,
                    edition.WinnerTier2.CreatedAt,
                    edition.WinnerTier2.UpdatedAt,
                    edition.WinnerTier2.Version
                ));
            }
            return winners;
        }
            
        public async Task MakeEditionWinnersPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id)
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

            edition.AreWinnersPublic = true;
            await _db.SaveChangesAsync();
        }

        public async Task MakeEditionWinnersNotPublicAsync(ClaimsPrincipal claimsPrincipal, Guid edition_id)
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

            edition.AreWinnersPublic = false;
            await _db.SaveChangesAsync();
        }

        public async Task<WinnerDto> UpsertEditionWinnerTier1Async(ClaimsPrincipal claimsPrincipal, Guid edition_id, WinnerUpserDto requestDto)
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

            var edition = await _db.Editions.Include(e => e.WinnerTier1).FirstOrDefaultAsync(e => e.Id == edition_id);
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition not found");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (edition.WinnerTier1Id == null || edition.WinnerTier1 == null)
            {
                Winner newWinner = new Winner
                {
                    Id = Guid.CreateVersion7(),
                    TeamName = requestDto.TeamName,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
                edition.WinnerTier1Id = newWinner.Id;
                edition.WinnerTier1 = newWinner;
                await _db.Winners.AddAsync(newWinner);
            }
            else
            {
                edition.WinnerTier1.TeamName = requestDto.TeamName;
                edition.WinnerTier1.UpdatedAt = timeNow;
                edition.WinnerTier1.Version++;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Winner data changed refresh your data");
            }

            return new WinnerDto
            (
                edition.WinnerTier1.Id,
                edition.Id,
                edition.WinnerTier1.TeamName,
                CompetitionTiers.TIER_1,
                edition.WinnerTier1.CreatedAt,
                edition.WinnerTier1.UpdatedAt,
                edition.WinnerTier1.Version
            );
        }

        public async Task<WinnerDto> UpsertEditionWinnerTier2Async(ClaimsPrincipal claimsPrincipal, Guid edition_id, WinnerUpserDto requestDto)
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

            var edition = await _db.Editions.Include(e => e.WinnerTier2).FirstOrDefaultAsync(e => e.Id == edition_id);
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition not found");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (edition.WinnerTier2Id == null || edition.WinnerTier2 == null)
            {
                Winner newWinner = new Winner
                {
                    Id = Guid.CreateVersion7(),
                    TeamName = requestDto.TeamName,
                    CreatedAt = timeNow,
                    UpdatedAt = timeNow,
                    Version = 1
                };
                edition.WinnerTier2Id = newWinner.Id;
                edition.WinnerTier2 = newWinner;
                await _db.Winners.AddAsync(newWinner);
            }
            else
            {
                edition.WinnerTier2.TeamName = requestDto.TeamName;
                edition.WinnerTier2.UpdatedAt = timeNow;
                edition.WinnerTier2.Version++;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Winner data changed refresh your data");
            }

            return new WinnerDto
            (
                edition.WinnerTier2.Id,
                edition.Id,
                edition.WinnerTier2.TeamName,
                CompetitionTiers.TIER_2,
                edition.WinnerTier2.CreatedAt,
                edition.WinnerTier2.UpdatedAt,
                edition.WinnerTier2.Version
            );
        }
    }
}
