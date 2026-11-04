using System.Security.Claims;
using koala.src.Modules.Cms.Data;
using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Entities;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;

namespace koala.src.Modules.Cms.Services
{
    public class PostService
    {
        private readonly CmsDbContext _db;

        public PostService(CmsDbContext db)
        {
            _db = db;
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

            //TODO: CHECK IF ACTIVE / REAL EDITION

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

            //FIXME: CHECK IF ACTIVE / REAL EDITION

            int affectedRows = await _db.Posts
                .Where(x => x.Id == id && x.Version == requestDto.Version)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.EditionId, requestDto.EditionId)
                    .SetProperty(x => x.Name, requestDto.Name)
                    .SetProperty(x => x.ContentJson, requestDto.ContentJson)
                    .SetProperty(x => x.IsVisible, requestDto.IsVisible)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(x => x.Version, x => x.Version + 1));

            if (affectedRows == 0)
            {
                bool isPost = await _db.Posts.AnyAsync(s => s.Id == id);
                if (!isPost)
                {
                    throw new KoalaException(KoalaErrorCodes.PostNotFound, "Specified post does not exist");
                }
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "The resource has been modified. Please refresh your data.");
            }


            var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

            if (post == null)
            {
                throw new KoalaException(KoalaErrorCodes.PostNotFound, "Post does not exist");
            }

            return new PostDto(post.Id, post.EditionId, post.Name, post.ContentJson, post.CreatedAt, post.UpdatedAt, post.IsVisible, post.Version);
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

            if (!isOrganizationAdmin && !isOrganizationEditor && (postQueryDto.ShowHidden == true))
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

            if (postQueryDto.ShowHidden != null)
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
