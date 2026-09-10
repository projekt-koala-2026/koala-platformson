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
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
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
                IsVisable = requestDto.IsVisable,
                Version = requestDto.Version
            };

            await _db.Posts.AddAsync(post);

            return new PostDto(post.Id,post.EditionId,post.Name,post.ContentJson,post.CreatedAt,post.UpdatedAt,post.IsVisable,post.Version);
        }

        public async Task<PostDto> UpdatePostAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UpdatePostRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id ==id);

            if(post == null)
            {
                throw new CmsException(CmsErrorCodes.PostNotFound, "Could not find post with provided id");
            }

            //TODO: CHECK IF ACTIVE / REAL EDITION

            DateTime timeNow = DateTime.UtcNow;

            post.EditionId = requestDto.EditionId;
            post.Name = requestDto.Name;
            post.ContentJson = requestDto.ContentJson;
            post.UpdatedAt = timeNow;
            post.IsVisable = requestDto.IsVisable;
            post.Version = requestDto.Version;

            await _db.SaveChangesAsync();

            return new PostDto(post.Id,post.EditionId,post.Name,post.ContentJson,post.CreatedAt,post.UpdatedAt,post.IsVisable,post.Version);
        }

        public async Task DeletePostAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new CmsException(CmsErrorCodes.Unauthenticated,"User not loged in");
            }
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            if(!isOrganizationAdmin && !isOrganizationEditor)
            {
                throw new CmsException(CmsErrorCodes.Forbiden, "This user canot create a sponsor");
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id ==id);

            if(post == null)
            {
                throw new CmsException(CmsErrorCodes.PostNotFound, "Could not find post with provided id");
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<PostDto>, ApiPagination)> GetPostsAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            //TODO: ADD A WAY TO FILLTER OUT DATA (VISIABLE AND SO ON)
            //bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            //bool isOrganizationEditor = ClaimsHelper.IsOrganizationEditor(claimsPrincipal);

            var posts = await _db.Posts.AsNoTracking().Where(p => p.IsVisable == true)
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
                        p.IsVisable,
                        p.Version
                    )
                ).Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber).Take(pageQueryDto.PageSize).ToListAsync();

            return (posts , new ApiPagination(pageQueryDto.PageNumber,pageQueryDto.PageSize,await _db.Koalicjants.AsNoTracking().CountAsync()));
        }
    }
}