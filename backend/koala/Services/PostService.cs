using koala.Data;
using koala.Data.Models;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace koala.Services
{
    public class PostService
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreatePostAsync(PostCreateVM vm)
        {
            var newPost = new Post
            {
                Id = Guid.NewGuid(),
                Title = vm.Title,
                MarkdownBody = vm.MarkdownBody,
                EditionId = vm.EditionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();
            return newPost;
        }

        public async Task<Post?> UpdatePostAsync(Guid id, PostUpdateVM vm)
        {
            var existingPost = await _context.Posts.FindAsync(id);
            if (existingPost == null) return null;

            existingPost.Title = vm.Title;
            existingPost.MarkdownBody = vm.MarkdownBody;
            existingPost.EditionId = vm.EditionId;

            await _context.SaveChangesAsync();
            return existingPost;
        }

        public async Task<bool> DeletePostAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Post?> GetPostByIdAsync(Guid id)
        {
            return await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
