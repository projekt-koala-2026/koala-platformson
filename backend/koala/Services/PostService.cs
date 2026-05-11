using koala.Data;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace koala.Services
{
    public class PostService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public PostService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<Post> CreatePostAsync(PostCreateVM vm)
        {
            var context = await _factory.CreateDbContextAsync();
            var newPost = new Post
            {
                Id = Guid.NewGuid(),
                Title = vm.Title,
                MarkdownBody = vm.MarkdownBody,
                EditionId = vm.EditionId,
                CreatedAt = DateTime.UtcNow
            };

            context.Posts.Add(newPost);
            await context.SaveChangesAsync();
            return newPost;
        }

        public async Task<Post?> UpdatePostAsync(Guid id, PostUpdateVM vm)
        {
            var context = await _factory.CreateDbContextAsync();
            var existingPost = await context.Posts.FindAsync(id);
            if (existingPost == null) return null;

            existingPost.Title = vm.Title;
            existingPost.MarkdownBody = vm.MarkdownBody;
            existingPost.EditionId = vm.EditionId;

            await context.SaveChangesAsync();
            return existingPost;
        }

        public async Task<bool> DeletePostAsync(Guid id)
        {
            var context = await _factory.CreateDbContextAsync();
            var post = await context.Posts.FindAsync(id);
            if (post == null) return false;

            context.Posts.Remove(post);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Post?> GetPostByIdAsync(Guid id)
        {
            var context = await _factory.CreateDbContextAsync();
            return await context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
