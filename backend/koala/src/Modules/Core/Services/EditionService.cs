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

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
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

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
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

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
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
                foreach(var e1s in edition.Etap1Series)
                {
                    _db.TaskItems.RemoveRange(e1s.TaskItems);
                }
            }

            await _db.SaveChangesAsync();
            await _accountModule.Internal_DeleteUserAndTeamDataAfterEditionCloses(id);

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
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
                .Select(e => new EditionDto(
                    e.Id,
                    e.Name,
                    e.State,
                    e.TeamRegisterOpen,
                    e.TeamRegisterClose,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.TaskFileId != null && e.TaskFileExtension != null ? true : false,
                    e.TaskPdfFileOpen,
                    e.TaskPdfFileClose,
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

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
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

            if (await _db.TaskSubmissions.Include(ts => ts.TaskPart).ThenInclude(tp => tp.TaskItem).AnyAsync(ts => ts.TaskPart.TaskItem.Etap1SeriesId == Etap1Series_id &&
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

        public async Task DeleteEditionTaskFileAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id)
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
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot add tasks file to a closed edition");
            }

            if (edition.TaskFileId == null && edition.TaskFileExtension == null)
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "This edition does not have a task file");
            }

            var id = edition.TaskFileId;
            var extension = edition.TaskFileExtension;
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
            edition.UpdatedAt = DateTime.UtcNow;
            edition.TaskFileId = null;
            edition.TaskFileExtension = null;
            await _db.SaveChangesAsync();
        }
        public async Task<string> GetEditionTaskFileAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isReviewer = ClaimsHelper.IsRoleOrganizationReviewer(claimsPrincipal);
            bool isCaptain = ClaimsHelper.IsRoleCaptain(claimsPrincipal);

            var edition = await _db.Editions.FirstOrDefaultAsync(e => e.Id == edition_id);

            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.Etap1SeriesNotFound, "Etap1Series of provided id does not exist or is not a part of the this edition");
            }

            if (edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot view task file of a closed edition try finding it in archived files");
            }

            DateTime timeNow = DateTime.UtcNow;
            if (!(isOrganizationAdmin || isReviewer) && (edition.TaskPdfFileOpen > timeNow || timeNow > edition.TaskPdfFileClose))
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Can only view task file while window for viewing it is open");
            }

            if (edition.TaskFileId == null && edition.TaskFileExtension == null)
            {
                throw new KoalaException(KoalaErrorCodes.FileNotFound, "This task item has no image to display");
            }
            var id = edition.TaskFileId;
            var extension = edition.TaskFileExtension;
            var path = $"{id}{extension}";
            return Path.Combine(_privateFilesPath, path);
        }
        public async Task<EditionDto> UpsertEditionTaskFileTimeWindowAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, EditionTaskFileUpserTimeWindoDto request)
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
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "edition of provided id does not exist");
            }

            if (edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot upsert task file time window for a close edition");
            }

            if (edition.TaskFileId == null && edition.TaskFileExtension == null)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot upsert task file time window for a edition without a task file");
            }

            DateTime timeNow = DateTime.UtcNow;

            edition.UpdatedAt = timeNow;
            edition.TaskPdfFileOpen = request.WindowOpen;
            edition.TaskPdfFileClose = request.WindowClose;
            edition.Version++;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                
            }

            return new EditionDto(
                edition.Id,
                edition.Name,
                edition.State,
                edition.TeamRegisterOpen,
                edition.TeamRegisterClose,
                edition.CreatedAt,
                edition.UpdatedAt,
                edition.TaskFileId != null &&edition.TaskFileExtension != null ? true : false,
                edition.TaskPdfFileOpen,
                edition.TaskPdfFileClose,
                edition.Version
            );
        }
        public async Task<string> UpsertEditionTaskFileAsync(ClaimsPrincipal? claimsPrincipal, Guid edition_id, EditionTaskFileUpserDto request)
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
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "edition of provided id does not exist");
            }

            if (edition.State == LifecycleStates.STATE_CLOSED)
            {
                throw new KoalaException(KoalaErrorCodes.Conflict, "Cannot upsert task file for a close edition");
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

            if (edition.Version != request.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Error make sure u have fresh data versions does not match");
            }

            if (edition.TaskFileId == null && edition.TaskFileExtension == null)
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
                    edition.TaskFileId = id;
                    edition.TaskFileExtension = extension;
                    edition.UpdatedAt = timeNow;
                    edition.TaskPdfFileOpen = request.WindowOpen;
                    edition.TaskPdfFileClose = request.WindowClose;
                    edition.Version++;
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
                var id = edition.TaskFileId;
                var oldExtension = edition.TaskFileExtension;
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
                    edition.TaskFileId = id;
                    edition.TaskFileExtension = newExtension;
                    edition.UpdatedAt = timeNow;
                    edition.TaskPdfFileOpen = request.WindowOpen;
                    edition.TaskPdfFileClose = request.WindowClose;
                    edition.Version++;
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
    }
}
