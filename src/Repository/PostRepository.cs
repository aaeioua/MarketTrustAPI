using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Data;
using MarketTrustAPI.Dtos.Post;
using MarketTrustAPI.Dtos.PropertyValue;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Models;
using MarketTrustAPI.SpatialIndexManager;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MarketTrustAPI.Repository
{
    /// <summary>
    /// Repository for managing posts.
    /// </summary>
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISpatialIndexManager<User> _spatialIndexManager;

        /// <summary>
        /// Constructs a new PostRepository.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="categoryRepository">The category repository.</param>
        /// <param name="spatialIndexManager">The spatial index manager.</param>
        public PostRepository(ApplicationDBContext context, ICategoryRepository categoryRepository, ISpatialIndexManager<User> spatialIndexManager)
        {
            _context = context;
            _categoryRepository = categoryRepository;
            _spatialIndexManager = spatialIndexManager;
        }

        /// <inheritdoc />
        public async Task<List<Post>> GetAllAsync(GetPostDto getPostDto)
        {
            IQueryable<Post> posts = _context.Posts.Include(post => post.PropertyValues);

            if (!string.IsNullOrEmpty(getPostDto.Title))
            {
                posts = posts.Where(post => EF.Functions.Like(post.Title, $"%{getPostDto.Title}%"));
            }

            if (!string.IsNullOrEmpty(getPostDto.Content))
            {
                posts = posts.Where(post => 
                    EF.Functions.Like(post.Content, $"%{getPostDto.Content}%") || 
                    post.PropertyValues.Any(propertyValue => 
                        EF.Functions.Like(propertyValue.Value, $"%{getPostDto.Content}%")
                    )
                );
            }

            if (getPostDto.CategoryId.HasValue)
            {
                List<Category> descendants = await _categoryRepository.GetDescendantsAsync(getPostDto.CategoryId.Value);
                List<int> descendantCategoryIds = descendants.Select(category => category.Id).ToList();
                descendantCategoryIds.Add(getPostDto.CategoryId.Value);

                posts = posts.Where(post => descendantCategoryIds.Contains(post.CategoryId));
            }

            if (getPostDto.Longitude.HasValue && getPostDto.Latitude.HasValue && getPostDto.SearchRadius.HasValue)
            {
                if (!_spatialIndexManager.IsInitialized())
                {
                    IEnumerable<User> users = await _context.Users.Where(user => user.Location != null && user.IsPublicLocation).ToListAsync();
                    _spatialIndexManager.Initialize(users);
                }

                Point center = new Point(getPostDto.Longitude.Value, getPostDto.Latitude.Value);
                HashSet<string> userIdsInRadius = _spatialIndexManager
                    .GetPointsInRadius(center, getPostDto.SearchRadius.Value)
                    .Select(user => user.Id)
                    .ToHashSet();

                posts = posts.Where(post => userIdsInRadius.Contains(post.UserId));
            }

            if (getPostDto.Page.HasValue && getPostDto.PageSize.HasValue)
            {
                int skip = (getPostDto.Page.Value - 1) * getPostDto.PageSize.Value;
                posts = posts.Skip(skip).Take(getPostDto.PageSize.Value);
            }

            return await posts.ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Post?> GetByIdAsync(int id)
        {
            return await _context.Posts
                .Include(post => post.PropertyValues)
                .FirstOrDefaultAsync(post => post.Id == id);
        }

        /// <inheritdoc />
        public async Task<Post> CreateAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<Post?> UpdateAsync(int id, UpdatePostDto updatePostDto)
        {
            Post? post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return null;
            }

            post.Title = updatePostDto.Title ?? post.Title;
            post.Content = updatePostDto.Content ?? post.Content;
            post.LastUpdatedAt = DateTime.Now;
            post.CategoryId = updatePostDto.CategoryId ?? post.CategoryId;
            post.Price = updatePostDto.Price ?? post.Price;
            post.Currency = updatePostDto.Currency ?? post.Currency;

            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<Post?> DeleteAsync(int id)
        {
            Post? post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return null;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<Post?> AddPropertyValueAsync(int id, PropertyValue propertyValue)
        {
            Post? post = await _context.Posts
                .Include(post => post.PropertyValues)
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post == null)
            {
                return null;
            }

            post.PropertyValues.Add(propertyValue);
            post.LastUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<Post?> UpdatePropertyValueAsync(int postId, int propertyValueId, UpdatePropertyValueDto updatePropertyValueDto)
        {
            Post? post = await _context.Posts
                .Include(post => post.PropertyValues)
                .FirstOrDefaultAsync(post => post.Id == postId);

            if (post == null)
            {
                return null;
            }

            PropertyValue? propertyValue = post.PropertyValues.FirstOrDefault(propertyValue => propertyValue.Id == propertyValueId);

            if (propertyValue == null)
            {
                return null;
            }

            propertyValue.Name = updatePropertyValueDto.Name ?? propertyValue.Name;
            propertyValue.Value = updatePropertyValueDto.Value ?? propertyValue.Value;
            post.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<Post?> DeletePropertyValueAsync(int postId, int propertyValueId)
        {
            Post? post = await _context.Posts
                .Include(post => post.PropertyValues)
                .FirstOrDefaultAsync(post => post.Id == postId);

            if (post == null)
            {
                return null;
            }

            PropertyValue? propertyValue = post.PropertyValues.FirstOrDefault(propertyValue => propertyValue.Id == propertyValueId);

            if (propertyValue == null)
            {
                return null;
            }

            post.PropertyValues.Remove(propertyValue);
            post.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return post;
        }

        /// <inheritdoc />
        public async Task<bool> ExistAsync(int id)
        {
            return await _context.Posts.AnyAsync(post => post.Id == id);
        }

        /// <inheritdoc />
        public async Task<bool> UserOwnsPostAsync(int postId, string userId)
        {
            return await _context.Posts.AnyAsync(post => post.Id == postId && post.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<bool> PropertyNameExistsAsync(int postId, string name)
        {
            return await _context.PropertyValues.AnyAsync(propertyValue => propertyValue.PostId == postId && propertyValue.Name.ToLower() == name.ToLower());
        }
    }
}