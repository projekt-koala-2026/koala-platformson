using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class PostService
    {
        private readonly CmsDbContext _db;
        private readonly ICoreModule _coreModule;

        public PostService(CmsDbContext db, ICoreModule coreModule)
        {
            _db = db;
            _coreModule = coreModule;
        }

        public async Task<PostDto> CreatePostAsync(ClaimsPrincipal? claimsPrincipal, CreatePostRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            await ValidateEditionAsync(requestDto.EditionId, requestDto.IsVisible, isCreate: true);

            DateTime timeNow = DateTime.UtcNow;

            Post post = new Post
            {
                Id = Guid.CreateVersion7(),
                EditionId = requestDto.EditionId,
                Name = requestDto.Name,
                ContentJson = requestDto.ContentJson,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                IsVisible = requestDto.IsVisible,
                Version = 1
            };

            await _db.Posts.AddAsync(post);
            await _db.SaveChangesAsync();

            return new PostDto(post.Id, post.EditionId, post.Name, post.ContentJson, post.CreatedAt, post.UpdatedAt, post.IsVisible, post.Version);
        }

        public async Task<PostDto> UpdatePostAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdatePostRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var post = await _db.Posts.FirstOrDefaultAsync(x => x.Id == id);
            if (post == null)
            {
                throw new KoalaException(KoalaErrorCodes.PostNotFound, "Specified post does not exist");
            }
            if (post.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }

            await ValidateEditionAsync(requestDto.EditionId, requestDto.IsVisible, isCreate: false, existingPostEditionId: post.EditionId);

            post.EditionId = requestDto.EditionId;
            post.Name = requestDto.Name;
            post.ContentJson = requestDto.ContentJson;
            post.IsVisible = requestDto.IsVisible;
            post.UpdatedAt = DateTime.UtcNow;
            post.Version++;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }

            return new PostDto(post.Id, post.EditionId, post.Name, post.ContentJson, post.CreatedAt, post.UpdatedAt, post.IsVisible, post.Version);
        }

        private async Task ValidateEditionAsync(Guid editionId, bool isVisible, bool isCreate, Guid? existingPostEditionId = null)
        {
            var edition = await _coreModule.Internal_GetEditionAsync(editionId);
            if (edition == null)
            {
                throw new KoalaException(KoalaErrorCodes.EditionNotFound, "Specified edition does not exist");
            }

            if (edition.State == LifecycleStates.STATE_CLOSED &&
                (isCreate || existingPostEditionId != editionId))
            {
                throw new KoalaException(KoalaErrorCodes.EditionInvalidStates, "Posts cannot be created or moved to a closed edition");
            }

            if (edition.State == LifecycleStates.STATE_INACTIVE && isVisible)
            {
                throw new KoalaException(KoalaErrorCodes.EditionInvalidStates, "Posts for inactive editions must remain hidden");
            }
        }

        public async Task DeletePostAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                throw new KoalaException(KoalaErrorCodes.PostNotFound, "Could not find post with provided id");
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<PostDto>, ApiPagination)> GetPostsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, PostQueryDto postQueryDto)
        {
            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor && (postQueryDto.ShowHidden == true || postQueryDto.ShowHidden == null))
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            var query = _db.Posts.AsNoTracking().AsQueryable();

            if (postQueryDto.EditionId != null)
            {
                query = query.Where(p => p.EditionId == postQueryDto.EditionId);
            }

            if (!string.IsNullOrEmpty(postQueryDto.Name))
            {
                query = query.Where(p => p.Name == postQueryDto.Name);
            }

            if (isOrganizationAdmin || isOrganizationEditor)
            {
                query = query.Where(p => p.IsVisible == !postQueryDto.ShowHidden);
            }

            var queryResults = await query.ToListAsync();
            var posts = queryResults
                .Select
                (
                    p => new PostDto
                    (
                        p.Id,
                        p.EditionId,
                        p.Name,
                        p.ContentJson,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.IsVisible,
                        p.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToList();

            return (posts, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }

        public async Task<PostDto> GetPostAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            var query = _db.Posts.AsNoTracking().AsQueryable();

            bool isOrganizationAdmin = ClaimsHelper.IsRoleOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsRoleOrganizationEditor(claimsPrincipal);

            if (!isOrganizationAdmin && !isOrganizationEditor)
            {
                query = query.Where(p => p.IsVisible == true);
            }

            var queryResults = await query.FirstOrDefaultAsync(p => p.Id == id);

            if (queryResults == null)
            {
                throw new KoalaException(KoalaErrorCodes.PostNotFound, "Post with this id does not exist");
            }

            PostDto post = new PostDto
            (
                queryResults.Id,
                queryResults.EditionId,
                queryResults.Name,
                queryResults.ContentJson,
                queryResults.CreatedAt,
                queryResults.UpdatedAt,
                queryResults.IsVisible,
                queryResults.Version
            );

            return post;
        }
    }
}
