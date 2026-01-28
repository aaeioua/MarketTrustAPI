using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.Post;
using MarketTrustAPI.Dtos.PropertyValue;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Mappers;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTrustAPI.Controllers
{
    /// <summary>
    /// Controller for managing posts.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IReputationService _reputationService;

        /// <summary>
        /// Constructs a new PostController.
        /// </summary>
        /// <param name="postRepository">The post repository.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="categoryRepository">The category repository.</param>
        /// <param name="reputationService">The reputation service.</param>
        public PostController(IPostRepository postRepository, IUserRepository userRepository, ICategoryRepository categoryRepository, IReputationService reputationService)
        {
            _postRepository = postRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _reputationService = reputationService;
        }

        /// <summary>
        /// Gets all posts based on the specified filters.
        /// </summary>
        /// <param name="getPostDto">The filters for retrieving posts.</param>
        /// <returns>A list of posts matching the filters.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PostDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetPostDto getPostDto)
        {
            // No authorization required, but if user is logged in, get their ID
            // to compute the personal trust of each of the posts
            string? trustorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            List<Post> posts = await _postRepository.GetAllAsync(getPostDto);
            List<PostDto> postDtos = new List<PostDto>(posts.Count);

            foreach (Post post in posts)
            {
                PostDto postDto = post.ToPostDto();
                postDto.GlobalTrust = await _reputationService.GetGlobalTrustAsync(post.UserId);
                postDto.PersonalTrust = trustorId != null && getPostDto.D.HasValue
                    ? await _reputationService.GetPersonalTrustAsync(trustorId, post.UserId, getPostDto.D.Value)
                    : null;

                postDtos.Add(postDto);
            }

            return Ok(postDtos);
        }

        /// <summary>
        /// Gets a post by its ID.
        /// </summary>
        /// <param name="id">The ID of the post to retrieve.</param>
        /// <param name="getPostByIdDto">The filters for retrieving the post.</param>
        /// <returns>The post with the specified ID, or a 404 Not Found if the post does not exist.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] GetPostByIdDto getPostByIdDto)
        {
            string? trustorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Post? post = await _postRepository.GetByIdAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            PostDto postDto = post.ToPostDto();
            postDto.GlobalTrust = await _reputationService.GetGlobalTrustAsync(post.UserId);
            postDto.PersonalTrust = trustorId != null && getPostByIdDto.D.HasValue
                ? await _reputationService.GetPersonalTrustAsync(trustorId, post.UserId, getPostByIdDto.D.Value)
                : null;

            return Ok(postDto);
        }

        /// <summary>
        /// Creates a new post.
        /// </summary>
        /// <param name="createPostDto">The data for creating the post.</param>
        /// <returns>The created post, or a 401 Unauthorized if the user ID is not found, or a 400 Bad Request if the user or category does not exist.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 201)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreatePostDto createPostDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _userRepository.ExistAsync(userId))
            {
                return BadRequest("User does not exist");
            }

            if (!await _categoryRepository.ExistAsync(createPostDto.CategoryId))
            {
                return BadRequest("Category does not exist");
            }

            if (createPostDto.Price.HasValue ^ createPostDto.Currency.HasValue)
            {
                return BadRequest("Both Price and Currency must be provided together");
            }

            if (createPostDto.Price.HasValue && createPostDto.Price < 0)
            {
                return BadRequest("Price cannot be negative");
            }

            if (createPostDto.Currency.HasValue && !Enum.IsDefined(typeof(Currency), createPostDto.Currency.Value))
            {
                return BadRequest("Invalid currency value");
            }

            Post post = createPostDto.ToPostFromCreateDto(userId, createPostDto.CategoryId);

            await _postRepository.CreateAsync(post);

            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post.ToPostDto());
        }

        /// <summary>
        /// Updates a post with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the post to update.</param>
        /// <param name="updatePostDto">The new post data.</param>
        /// <returns>The updated post, or a 401 Unauthorized if the user ID is not found or the user is not the owner of the post, or a 400 Bad Request if the category does not exist, or a 404 Not Found if the post does not exist.</returns>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdatePostDto updatePostDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _postRepository.UserOwnsPostAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the post");
            }

            if (updatePostDto.CategoryId != null && !await _categoryRepository.ExistAsync(updatePostDto.CategoryId.Value))
            {
                return BadRequest("Category does not exist");
            }

            if (updatePostDto.Price.HasValue ^ updatePostDto.Currency.HasValue)
            {
                return BadRequest("Both Price and Currency must be provided together");
            }

            if (updatePostDto.Price.HasValue && updatePostDto.Price < 0)
            {
                return BadRequest("Price cannot be negative");
            }

            if (updatePostDto.Currency.HasValue && !Enum.IsDefined(typeof(Currency), updatePostDto.Currency.Value))
            {
                return BadRequest("Invalid currency value");
            }

            Post? post = await _postRepository.UpdateAsync(id, updatePostDto);

            if (post == null)
            {
                return NotFound("Post not found");
            }

            return Ok(post.ToPostDto());
        }

        /// <summary>
        /// Deletes a post with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the post to delete.</param>
        /// <returns>The deleted post, or a 401 Unauthorized if the user ID is not found or the user is not the owner of the post, or a 404 Not Found if the post does not exist.</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _postRepository.UserOwnsPostAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the post");
            }

            Post? post = await _postRepository.DeleteAsync(id);

            if (post == null)
            {
                return NotFound("Post not found");
            }

            return Ok(post.ToPostDto());
        }

        /// <summary>
        /// Adds a property value to a post.
        /// </summary>
        /// <param name="id">The ID of the post to add the property value to.</param>
        /// <param name="addPropertyValueDto">The property value to add.</param>
        /// <returns>The post with the added property value, or a 401 Unauthorized if the user ID is not found or the user is not the owner of the post, or a 400 Bad Request if the property name already exists, or a 404 Not Found if the post does not exist.</returns>
        [HttpPost("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddPropertyValue([FromRoute] int id, [FromBody] AddPropertyValueDto addPropertyValueDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _postRepository.UserOwnsPostAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the post");
            }

            if (await _postRepository.PropertyNameExistsAsync(id, addPropertyValueDto.Name))
            {
                return BadRequest("Property name already exists");
            }

            PropertyValue propertyValue = addPropertyValueDto.ToPropertyValueFromAddDto(id);

            Post? post = await _postRepository.AddPropertyValueAsync(id, propertyValue);

            if (post == null)
            {
                return NotFound("Post not found");
            }

            return Ok(post.ToPostDto());
        }

        /// <summary>
        /// Updates a property value of a post.
        /// </summary>
        /// <param name="postId">The ID of the post containing the property value.</param>
        /// <param name="propertyValueId">The ID of the property value to update.</param>
        /// <param name="updatePropertyValueDto">The new property value data.</param>
        /// <returns>The post with the updated property value, or a 401 Unauthorized if the user ID is not found or the user is not the owner of the post, or a 404 Not Found if the post or property value does not exist.</returns>
        [HttpPut("{postId:int}/{propertyValueId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePropertyValue([FromRoute] int postId, [FromRoute] int propertyValueId, [FromBody] UpdatePropertyValueDto updatePropertyValueDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _postRepository.UserOwnsPostAsync(postId, userId))
            {
                return Unauthorized("User is not the owner of the post");
            }

            Post? post = await _postRepository.UpdatePropertyValueAsync(postId, propertyValueId, updatePropertyValueDto);

            if (post == null)
            {
                return NotFound("Post or property not found");
            }

            return Ok(post.ToPostDto());
        }

        /// <summary>
        /// Deletes a property value from a post.
        /// </summary>
        /// <param name="postId">The ID of the post containing the property value.</param>
        /// <param name="propertyValueId">The ID of the property value to delete.</param>
        /// <returns>The post with the deleted property value, or a 401 Unauthorized if the user ID is not found or the user is not the owner of the post, or a 404 Not Found if the post or property value does not exist.</returns>
        [HttpDelete("{postId:int}/{propertyValueId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(PostDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePropertyValue([FromRoute] int postId, [FromRoute] int propertyValueId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _postRepository.UserOwnsPostAsync(postId, userId))
            {
                return Unauthorized("User is not the owner of the post");
            }

            Post? post = await _postRepository.DeletePropertyValueAsync(postId, propertyValueId);

            if (post == null)
            {
                return NotFound("Post or property not found");
            }

            return Ok(post.ToPostDto());
        }
    }
}