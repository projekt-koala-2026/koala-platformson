using System.Security.Claims;
using koala.src.Modules.Core.Data;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Entities;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;

namespace koala.src.Modules.Core.Services
{
    public interface ITaskItemFileOperations
    {
        void CreateDirectory(string path);
        void Move(string source_path, string destination_path, bool overwrite);
        Task WriteAsync(string path, IFormFile file, bool overwrite);
        Task<byte[]> ReadAllBytesAsync(string path);
        bool Exists(string path);
        void Delete(string path);
    }

    internal sealed class PhysicalTaskItemFileOperations : ITaskItemFileOperations
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public async Task WriteAsync(string path, IFormFile file, bool overwrite)
        {
            await using var stream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }

        public Task<byte[]> ReadAllBytesAsync(string path) => File.ReadAllBytesAsync(path);
        public bool Exists(string path) => File.Exists(path);
        public void Delete(string path) => File.Delete(path);
        public void Move(string source_path, string destination_path, bool overwrite) => File.Move(source_path, destination_path, overwrite);
    }
    public class EditionService
    {
        private readonly CoreDbContext _db;
        private readonly IAccountModule _accountModule;
        private readonly string _privateFilesPath;
        private readonly ITaskItemFileOperations _fileOperations;

        public EditionService(CoreDbContext db, IAccountModule accountModule, string privateFilesPath)
            : this(db, accountModule, privateFilesPath, new PhysicalTaskItemFileOperations())
        {
        }

        public EditionService(CoreDbContext db, IAccountModule accountModule, string privateFilesPath, ITaskItemFileOperations fileOperations)
        {
            _db = db;
            _accountModule = accountModule;
            _privateFilesPath = privateFilesPath;
            _fileOperations = fileOperations;
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
                State = LifecycleStates.STATE_INACTIVE,
                TeamRegisterOpen = createEditionDto.TeamRegisterOpen,
                TeamRegisterClose = createEditionDto.TeamRegisterClose,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var editionTier1TotalRanking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                Name = "Etap1 - ranking suma serii szkoły podstawowe",
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };
            var editionTier2TotalRanking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                Name = "Etap1 - ranking suma serii szkoły ponadpodstawowe",
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            edition.AreRankingsPublic = false;
            edition.Tier1RankingTotalId = editionTier1TotalRanking.RankingId;
            edition.Tier2RankingTotalId = editionTier2TotalRanking.RankingId;

            await _db.Rankings.AddRangeAsync([editionTier1TotalRanking, editionTier2TotalRanking]);
            await _db.Editions.AddAsync(edition);
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

            bool isAnyEditionActive = await _db.Editions.AnyAsync(e => e.State == LifecycleStates.STATE_ACTIVE);
            if (isAnyEditionActive)
            {
                throw new KoalaException(KoalaErrorCodes.ActiveEditionAlreadyExists, "There already exists another editions that is curently active canot have two editions active at once");
            }

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State == LifecycleStates.STATE_INACTIVE);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be activated becouse it was closed or is active already");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.State = LifecycleStates.STATE_ACTIVE;
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

            var edition = await _db.Editions
                .Include(e => e.Etap1Series)
                .ThenInclude(se => se.TaskItems)
                .ThenInclude(ti => ti.TaskAnswers)
                .Include(e => e.Etap1Series)
                .ThenInclude(se => se.TaskItems)
                .ThenInclude(ti => ti.TaskSubmissions)
                .FirstOrDefaultAsync(e => e.Id == id && e.State == LifecycleStates.STATE_ACTIVE);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition of provided id does not exist or canot be closed becouse it is not active");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.State = LifecycleStates.STATE_CLOSED;
            edition.UpdatedAt = timeNow;

            if (edition != null)
            {
                foreach (var Etap1Series in edition.Etap1Series)
                {
                    foreach (var taskItem in Etap1Series.TaskItems)
                    {
                        if (taskItem.TaskAnswers != null && taskItem.TaskAnswers.Any())
                        {
                            _db.TaskAnswers.RemoveRange(taskItem.TaskAnswers);
                        }

                        if (taskItem.TaskSubmissions != null && taskItem.TaskSubmissions.Any())
                        {
                            _db.TaskSubmissions.RemoveRange(taskItem.TaskSubmissions);
                        }

                        if (taskItem.ImageId != null && taskItem.ImageType != null)
                        {
                            var physicalPath = Path.Combine(_privateFilesPath, $"{taskItem.ImageId}{taskItem.ImageType}");

                            try
                            {
                                if (_fileOperations.Exists(physicalPath))
                                {
                                    _fileOperations.Delete(physicalPath);
                                }
                            }
                            catch
                            {

                            }
                        }
                        _db.TaskItems.Remove(taskItem);
                    }
                }
            }

            await _db.SaveChangesAsync();
            await _accountModule.Internal_DeleteUserAndTeamDataAfterEditionCloses(id);

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
                        if (state != LifecycleStates.STATE_ACTIVE && state != LifecycleStates.STATE_INACTIVE && state != LifecycleStates.STATE_CLOSED)
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
            var edition = await _db.Editions.AsNoTracking().FirstOrDefaultAsync(e => e.State == LifecycleStates.STATE_ACTIVE);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "There is no current active edition at the moment");
            }

            return new EditionDto(edition.Id, edition.Name, edition.State, edition.TeamRegisterOpen, edition.TeamRegisterClose, edition.CreatedAt, edition.UpdatedAt, edition.Version);
        }

        public async Task<Etap1SeriesDto> CreateEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, Etap1SeriesCreateDto createEtap1SeriesDto)
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

            if (edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Etap1Series can only be created for the active and inactive edition");
            }

            if (createEtap1SeriesDto.AnswerOpen >= createEtap1SeriesDto.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Answer opening must be before closing");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Name == createEtap1SeriesDto.Name && e.EditionId == id);

            if (Etap1Series != null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesAlreadyExists, "Etap1Series with this name already exists canot create a new one");
            }

            DateTime timeNow = DateTime.UtcNow;


            Etap1Series newEtap1Series = new Etap1Series
            {
                Id = Guid.CreateVersion7(),
                EditionId = id,
                Name = createEtap1SeriesDto.Name,
                AnswerOpen = createEtap1SeriesDto.AnswerOpen,
                AnswerClose = createEtap1SeriesDto.AnswerClose,
                State = LifecycleStates.STATE_INACTIVE,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            var Tier1Ranking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                Name = newEtap1Series.Name + "- szkoły podstawowe",
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };
            var Tier2Ranking = new Ranking
            {
                RankingId = Guid.CreateVersion7(),
                Name = newEtap1Series.Name + "- szkoły ponad podstawowe",
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1
            };

            newEtap1Series.AreRankingsPublic = false;
            newEtap1Series.Tier1RankingId = Tier1Ranking.RankingId;
            newEtap1Series.Tier2RankingId = Tier2Ranking.RankingId;
            await _db.Rankings.AddRangeAsync([Tier1Ranking, Tier2Ranking]);
            await _db.Etap1Series.AddAsync(newEtap1Series);
            await _db.SaveChangesAsync();

            return new Etap1SeriesDto
            (
                newEtap1Series.Id,
                newEtap1Series.EditionId,
                newEtap1Series.Name,
                newEtap1Series.AnswerOpen,
                newEtap1Series.AnswerClose,
                newEtap1Series.State,
                newEtap1Series.CreatedAt,
                newEtap1Series.UpdatedAt,
                newEtap1Series.Version
            );
        }

        public async Task<Etap1SeriesDto> UpdateEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Etap1SeriesUpdateDto updateEtap1SeriesDto)
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

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id && e.State != LifecycleStates.STATE_CLOSED);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Edition does not exist or is closed");
            }

            bool isEtap1SeriesWithNewNameAlreadyInDb = await _db.Etap1Series.AnyAsync(e => e.Name == updateEtap1SeriesDto.Name && e.Id != Etap1Series_id && e.EditionId == edition_id);

            if (isEtap1SeriesWithNewNameAlreadyInDb == true)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesAlreadyExists, "Etap1Series with this name already exist thus canot update this Etap1Series name to it");
            }

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            if (Etap1Series.Version != updateEtap1SeriesDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Etap1Series versions do not match make sure u have the freshest data");
            }

            if (updateEtap1SeriesDto.AnswerOpen >= updateEtap1SeriesDto.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Answer opening must be before closing");
            }

            DateTime timeNow = DateTime.UtcNow;

            Etap1Series.Name = updateEtap1SeriesDto.Name;
            Etap1Series.AnswerOpen = updateEtap1SeriesDto.AnswerOpen;
            Etap1Series.AnswerClose = updateEtap1SeriesDto.AnswerClose;
            Etap1Series.UpdatedAt = timeNow;
            Etap1Series.Version++;
            await _db.SaveChangesAsync();

            return new Etap1SeriesDto
            (
                Etap1Series.Id,
                Etap1Series.EditionId,
                Etap1Series.Name,
                Etap1Series.AnswerOpen,
                Etap1Series.AnswerClose,
                Etap1Series.State,
                Etap1Series.CreatedAt,
                Etap1Series.UpdatedAt,
                Etap1Series.Version
            );
        }

        public async Task<Etap1SeriesDto> GetEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id)
        {
            var Etap1Series = await _db.Etap1Series
                .AsNoTracking()
                .Include(e => e.Edition)
                .FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.Edition.Id == edition_id);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series not found");
            }

            return new Etap1SeriesDto
            (
                Etap1Series.Id,
                Etap1Series.EditionId,
                Etap1Series.Name,
                Etap1Series.AnswerOpen,
                Etap1Series.AnswerClose,
                Etap1Series.State,
                Etap1Series.CreatedAt,
                Etap1Series.UpdatedAt,
                Etap1Series.Version
            );
        }

        public async Task<(List<Etap1SeriesDto>, ApiPagination)> GetEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, PageQueryDto pageQueryDto)
        {
            var query = _db.Etap1Series
                .AsNoTracking()
                .Where(se => se.EditionId == edition_id);

            var totalCount = await query.CountAsync();

            var Etap1Series = await query
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new Etap1SeriesDto(
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

            return (Etap1Series, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task DeleteEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id)
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

            var Etap1Series = await _db.Etap1Series
                .Include(e => e.Tier1Ranking)
                .Include(e => e.Tier2Ranking)
                .FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id && e.State == LifecycleStates.STATE_INACTIVE);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is active or closed can only delete the inactive Etap1Series");
            }

            _db.Etap1Series.Remove(Etap1Series);
            _db.Rankings.RemoveRange(Etap1Series.Tier1Ranking, Etap1Series.Tier2Ranking);
            await _db.SaveChangesAsync();
        }

        public async Task<Etap1SeriesDto> ActivateEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id && e.State != LifecycleStates.STATE_CLOSED);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is closed");
            }

            Etap1Series.State = LifecycleStates.STATE_ACTIVE;
            await _db.SaveChangesAsync();

            return new Etap1SeriesDto
            (
                Etap1Series.Id,
                Etap1Series.EditionId,
                Etap1Series.Name,
                Etap1Series.AnswerOpen,
                Etap1Series.AnswerClose,
                Etap1Series.State,
                Etap1Series.CreatedAt,
                Etap1Series.UpdatedAt,
                Etap1Series.Version
            );
        }

        public async Task<Etap1SeriesDto> CloseEtap1SeriesAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id)
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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id && e.State == LifecycleStates.STATE_ACTIVE);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not active");
            }

            if (DateTime.UtcNow <= Etap1Series.AnswerClose)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot close the Etap1Series before the answer deadline");

            if (await _db.TaskSubmissions.AnyAsync(ts => ts.TaskItem.Etap1SeriesId == Etap1Series_id &&
                (ts.Reviewer1Id == null || ts.Reviewer2Id == null || ts.Reviewer1Id == ts.Reviewer2Id ||
                 ts.Review1Score == null || ts.Review2Score == null || ts.Review1Score != ts.Review2Score ||
                 ts.AutoReviewState == null)))
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot close the Etap1Series until all submitted answers have two matching reviews");

            Etap1Series.State = LifecycleStates.STATE_CLOSED;
            Etap1Series.UpdatedAt = DateTime.UtcNow;
            Etap1Series.Version++;
            await _db.SaveChangesAsync();

            return new Etap1SeriesDto
            (
                Etap1Series.Id,
                Etap1Series.EditionId,
                Etap1Series.Name,
                Etap1Series.AnswerOpen,
                Etap1Series.AnswerClose,
                Etap1Series.State,
                Etap1Series.CreatedAt,
                Etap1Series.UpdatedAt,
                Etap1Series.Version
            );
        }

        public async Task<TaskItemDto> CreateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, TaskItemCreateDto createTaskDto)
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

            var Etap1Series = await _db.Etap1Series.Include(e => e.Edition).FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (Etap1Series.State == LifecycleStates.STATE_CLOSED || Etap1Series.Edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot add tasks to a closed Etap1Series");
            }

            var newTask = await _db.TaskItems.FirstOrDefaultAsync(t => t.Name == createTaskDto.Name && t.Etap1SeriesId == Etap1Series_id);

            if (newTask != null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskAlreadyExists, "Task with this name already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            newTask = new TaskItem
            {
                Id = Guid.CreateVersion7(),
                Etap1SeriesId = Etap1Series_id,
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
                newTask.Etap1SeriesId,
                newTask.Name,
                newTask.Tier,
                newTask.ContentJson,
                (newTask.ImageId == null || newTask.ImageType == null) ? false : true,
                newTask.CreatedAt,
                newTask.UpdatedAt,
                newTask.Version
            );
        }
        private void TryDeletePhysicalFile(string path)
        {
            try
            {
                if (_fileOperations.Exists(path))
                {
                    _fileOperations.Delete(path);
                }
            }
            catch
            {
                // Preserve the original database error if best-effort file cleanup also fails.
            }
        }

        public async Task DeleteTaskItemImageAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid taskItem_id)
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

            var Etap1Series = await _db.Etap1Series.Include(e => e.Edition).FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (Etap1Series.State == LifecycleStates.STATE_CLOSED || Etap1Series.Edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot add tasks to a closed Etap1Series");
            }

            var taskItem = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskItem_id && t.Etap1SeriesId == Etap1Series_id);

            if (taskItem == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task not found");
            }

            if (taskItem.ImageId == null && taskItem.ImageType == null)
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "This task item has no image to display");
            }

            var id = taskItem.ImageId;
            var extension = taskItem.ImageType;
            var path = $"{id}{extension}";
            var physicalPath = Path.Combine(_privateFilesPath, path);
            try
            {
                if (_fileOperations.Exists(physicalPath))
                {
                    _fileOperations.Delete(physicalPath);
                }
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "Error deleting the physical file from disk");
            }
            taskItem.UpdatedAt = DateTime.UtcNow;
            taskItem.ImageId = null;
            taskItem.ImageType = null;
            await _db.SaveChangesAsync();
        }
        public async Task<string> GetTaskItemImageAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid taskItem_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);
            bool isCaptain = ClaimsHelper.IsRoleCaptain(claimsPrincipal);

            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            }

            if (!isOrganizationAdmin && !isCaptain && !isReviewer)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation on resource");
            }

            var Etap1Series = await _db.Etap1Series.Include(e => e.Edition).FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (Etap1Series.State == LifecycleStates.STATE_CLOSED || Etap1Series.Edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot view task image of a closed Etap1Series");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (isCaptain && (Etap1Series.AnswerOpen > timeNow || timeNow > Etap1Series.AnswerClose))
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Captain can only view image while window for answers is open");
            }

            var taskItem = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskItem_id && t.Etap1SeriesId == Etap1Series_id);

            if (taskItem == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task not found");
            }

            if (taskItem.ImageId == null && taskItem.ImageType == null)
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "This task item has no image to display");
            }
            var id = taskItem.ImageId;
            var extension = taskItem.ImageType;
            var path = $"{id}{extension}";
            return Path.Combine(_privateFilesPath, path);
        }
        public async Task<string> UpsertTaskItemImageAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid taskItem_id, TaskItemImageUpsertDto request)
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

            var Etap1Series = await _db.Etap1Series.Include(e => e.Edition).FirstOrDefaultAsync(e => e.Id == Etap1Series_id && e.EditionId == edition_id);

            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (Etap1Series.State == LifecycleStates.STATE_CLOSED || Etap1Series.Edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot add tasks to a closed Etap1Series");
            }

            var taskItem = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskItem_id && t.Etap1SeriesId == Etap1Series_id);

            if (taskItem == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task not found");
            }

            DateTime timeNow = DateTime.UtcNow;

            try
            {
                _fileOperations.CreateDirectory(_privateFilesPath);
            }
            catch
            {
                throw new KoalaException(KoalaErrorCodes.FolderCreationFailure, "Error when creating a folder holding private files");
            }

            if (taskItem.Version != request.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Error make sure u have fresh data versions does not match");
            }

            if (taskItem.ImageId == null && taskItem.ImageType == null)
            {
                try
                {
                    _fileOperations.CreateDirectory(_privateFilesPath);
                }
                catch
                {
                    throw new KoalaException(KoalaErrorCodes.FolderCreationFailure, "Error when creating a folder holding private files");
                }

                var id = Guid.CreateVersion7();
                var extension = Path.GetExtension(request.File.FileName);
                var path = $"{id}{extension}";
                var physicalPath = Path.Combine(_privateFilesPath, path);

                try
                {
                    await _fileOperations.WriteAsync(physicalPath, request.File, overwrite: false);
                }
                catch
                {
                    throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
                }
                try
                {
                    taskItem.ImageId = id;
                    taskItem.ImageType = extension;
                    taskItem.UpdatedAt = timeNow;
                    await _db.SaveChangesAsync();
                }
                catch
                {
                    TryDeletePhysicalFile(physicalPath);
                    throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving public file record to database");
                }

                return physicalPath;
            }
            else
            {
                var temp_id = Guid.CreateVersion7();
                var id = taskItem.ImageId;
                var oldExtension = taskItem.ImageType;
                var newExtension = Path.GetExtension(request.File.FileName);

                var oldPhysicalPath = Path.Combine(_privateFilesPath, $"{id}{oldExtension}");
                var newPhysicalPath = Path.Combine(_privateFilesPath, $"{id}{newExtension}");
                var tempPhysicalPath = Path.Combine(_privateFilesPath, $"{id}_{temp_id}.tmp");

                try
                {
                    await _fileOperations.WriteAsync(tempPhysicalPath, request.File, overwrite: true);
                }
                catch
                {
                    TryDeletePhysicalFile(tempPhysicalPath);
                    throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error creating a private file");
                }

                try
                {
                    taskItem.ImageId = id;
                    taskItem.ImageType = newExtension;
                    taskItem.UpdatedAt = timeNow;
                    await _db.SaveChangesAsync();
                }
                catch
                {
                    TryDeletePhysicalFile(tempPhysicalPath);
                    throw new KoalaException(KoalaErrorCodes.DatabaseError, "Error saving private file record to database");
                }

                try
                {
                    _fileOperations.Move(tempPhysicalPath, newPhysicalPath, overwrite: true);
                }
                catch
                {
                    throw new KoalaException(KoalaErrorCodes.FileCreationFailure, "Error finalizing private file update");
                }

                if (!string.Equals(oldExtension, newExtension, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeletePhysicalFile(oldPhysicalPath);
                }

                await _db.SaveChangesAsync();

                return newPhysicalPath;
            }


        }

        public async Task<TaskItemDto> UpdateTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid task_id, TaskItemUpdateDto request)
        {
            if (!ClaimsHelper.IsAuthenticated(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User not logged in");
            if (!ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only administrators can update tasks");
            if (string.IsNullOrWhiteSpace(request.Name) || (request.Tier != CompetitionTiers.TIER_1 && request.Tier != CompetitionTiers.TIER_2))
                throw new KoalaException(KoalaErrorCodes.BadRequest, "Task name and a valid tier are required");
            try { using var content = System.Text.Json.JsonDocument.Parse(request.ContentJson); }
            catch (System.Text.Json.JsonException) { throw new KoalaException(KoalaErrorCodes.BadRequest, "Task content must be valid JSON"); }

            var task = await _db.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == task_id && t.Etap1SeriesId == Etap1Series_id && t.Etap1Series.EditionId == edition_id);
            if (task == null)
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task does not exist in the specified Etap1Series");
            if (await _db.TaskItems.AnyAsync(t => t.Etap1SeriesId == Etap1Series_id && t.Name == request.Name && t.Id != task_id))
                throw new KoalaException(KoalaErrorCodes.TaskAlreadyExists, "A task with this name already exists in the Etap1Series");
            var now = DateTime.UtcNow;
            var updated = await _db.TaskItems.Where(t => t.Id == task_id && t.Etap1SeriesId == Etap1Series_id &&
                    t.Etap1Series.EditionId == edition_id && t.Etap1Series.State != LifecycleStates.STATE_CLOSED &&
                    t.Version == request.Version && !_db.TaskSubmissions.Any(s => s.TaskId == task_id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Name, request.Name)
                    .SetProperty(t => t.Tier, request.Tier).SetProperty(t => t.ContentJson, request.ContentJson)
                    .SetProperty(t => t.UpdatedAt, now).SetProperty(t => t.Version, t => t.Version + 1));
            if (updated == 0)
                throw new KoalaException(KoalaErrorCodes.Conflict, "The task changed, is closed, or already has submissions");
            return new TaskItemDto(task.Id, task.Etap1SeriesId, request.Name, request.Tier, request.ContentJson, (task.ImageId == null || task.ImageType == null) ? false : true, task.CreatedAt, now, request.Version + 1);
        }

        public async Task DeleteTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid task_id)
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

            var task = await _db.TaskItems.FirstOrDefaultAsync(e => e.Id == task_id && e.Etap1SeriesId == Etap1Series_id && e.Etap1Series.EditionId == edition_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            var deleted = await _db.TaskItems.Where(t => t.Id == task_id && t.Etap1SeriesId == Etap1Series_id &&
                t.Etap1Series.EditionId == edition_id && t.Etap1Series.State != LifecycleStates.STATE_CLOSED &&
                !_db.TaskSubmissions.Any(s => s.TaskId == task_id)).ExecuteDeleteAsync();
            if (deleted == 0)
                throw new KoalaException(KoalaErrorCodes.Conflict, "Task is closed or already has submissions");
        }

        public async Task<TaskItemDto> GetTaskItemAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid task_id)
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
                .Include(e => e.Etap1Series)
                .ThenInclude(e => e.Edition)
                .FirstOrDefaultAsync(e => e.Id == task_id && e.Etap1SeriesId == Etap1Series_id && e.Etap1Series.EditionId == edition_id);

            if (task == null)
            {
                throw new KoalaException(KoalaErrorCodes.TaskNotFound, "Task of provided id does not exist");
            }

            return new TaskItemDto
            (
                task.Id,
                task.Etap1SeriesId,
                task.Name,
                task.Tier,
                task.ContentJson,
                (task.ImageId == null || task.ImageType == null) ? false : true,
                task.CreatedAt,
                task.UpdatedAt,
                task.Version
            );
        }

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTaskItemsAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, PageQueryDto pageQueryDto)
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
                .Where(e => e.Etap1SeriesId == Etap1Series_id && e.Etap1Series.EditionId == edition_id);

            var totalCount = await query.CountAsync();
            var entities = await query
                .Include(e => e.Etap1Series)
                .ThenInclude(e => e.Edition)
                .OrderBy(e => e.Id)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToListAsync();

            var TaskItems = entities.Where(e => e.Etap1Series.Id == Etap1Series_id && e.Etap1Series.Edition.Id == edition_id).Select(e => new TaskItemDto
            (
                e.Id,
                e.Etap1SeriesId,
                e.Name,
                e.Tier,
                e.ContentJson,
                (e.ImageId == null || e.ImageType == null) ? false : true,
                e.CreatedAt,
                e.UpdatedAt,
                e.Version
            )).ToList();

            return (TaskItems, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }

        public async Task<(List<TaskItemDto> Data, ApiPagination Pagination)> GetTasksActiveAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, Guid Etap1Series_id, Guid team_id, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isCaptain = claimsPrincipal?.IsInRole(RoleNames.NAME_TEAM_CAPTAIN) == true;

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

            var Etap1Series = await _db.Etap1Series.FirstOrDefaultAsync(se => se.Id == Etap1Series_id);
            if (Etap1Series == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Provided Etap1Series does not exist");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (Etap1Series.AnswerOpen > timeNow || timeNow > Etap1Series.AnswerClose)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Teams cant in this time answer/view tasks");
            }

            var query = _db.TaskItems.AsNoTracking()
                .Where(e => e.Etap1SeriesId == Etap1Series_id
                        && e.Etap1Series.State == "ACTIVE"
                        && e.Etap1Series.EditionId == edition_id
                        && e.Etap1Series.Edition.State == "ACTIVE");

            if (teamInfo.Tier == CompetitionTiers.TIER_1)
            {
                query = query.Where(e => e.Tier == CompetitionTiers.TIER_1);
            }
            if (teamInfo.Tier == CompetitionTiers.TIER_2)
            {
                query = query.Where(e => e.Tier == CompetitionTiers.TIER_1 || e.Tier == CompetitionTiers.TIER_2);
            }

            var totalCount = await query.CountAsync();

            var taskItems = await query
                .OrderBy(e => e.CreatedAt)
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .Select(e => new TaskItemDto(
                    e.Id,
                    e.Etap1SeriesId,
                    e.Name,
                    e.Tier,
                    e.ContentJson,
                    (e.ImageId == null || e.ImageType == null) ? false : true,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.Version
                ))
                .ToListAsync();

            return (taskItems, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, totalCount));
        }
    }
}
