using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.TrustRating;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Mappers;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTrustAPI.Controllers
{
    /// <summary>
    /// Controller for managing trust ratings.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TrustRatingController : ControllerBase
    {
        private readonly ITrustRatingRepository _trustRatingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;

        /// <summary>
        /// Constructs a new TrustRatingController.
        /// </summary>
        /// <param name="trustRatingRepository">The trust rating repository.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="postRepository">The post repository.</param>
        public TrustRatingController(ITrustRatingRepository trustRatingRepository, IUserRepository userRepository, IPostRepository postRepository)
        {
            _trustRatingRepository = trustRatingRepository;
            _userRepository = userRepository;
            _postRepository = postRepository;
        }

        /// <summary>
        /// Gets all trust ratings based on the specified filters.
        /// </summary>
        /// <param name="getTrustRatingDto">The filters for retrieving trust ratings.</param>
        /// <returns>A list of trust ratings matching the filters, or a 401 Unauthorized if the user ID is not found.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<TrustRatingDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] GetTrustRatingDto getTrustRatingDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            List<TrustRating> trustRatings = await _trustRatingRepository.GetAllAsync(getTrustRatingDto, userId);
            List<TrustRatingDto> trustRatingDtos = trustRatings
                .Select(trustRating => trustRating.ToTrustRatingDto())
                .ToList();

            return Ok(trustRatingDtos);
        }

        /// <summary>
        /// Gets a trust rating by its ID.
        /// </summary>
        /// <param name="id">The ID of the trust rating to retrieve.</param>
        /// <returns>The trust rating with the specified ID, or a 404 Not Found if the trust rating does not exist, or a 401 Unauthorized if the user ID is not found or the user does not own the trust rating.</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(TrustRatingDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }
            else if (!await _trustRatingRepository.UserOwnsTrustRatingAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the trust rating or the trust rating does not exist");
            }

            TrustRating? trustRating = await _trustRatingRepository.GetByIdAsync(id);

            if (trustRating == null)
            {
                return NotFound();
            }

            return Ok(trustRating.ToTrustRatingDto());
        }

        /// <summary>
        /// Creates a new trust rating.
        /// </summary>
        /// <param name="createTrustRatingDto">The data for the new trust rating.</param>
        /// <returns>The created trust rating, or a 401 Unauthorized if the user ID is not found, a 400 Bad Request if the user tries to rate themselves, or a 404 Not Found if the trustee or post does not exist.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(TrustRatingDto), 201)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Create([FromBody] CreateTrustRatingDto createTrustRatingDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (createTrustRatingDto.TrusteeId == userId)
            {
                return BadRequest("User cannot rate self");
            }

            if (!await _userRepository.ExistAsync(createTrustRatingDto.TrusteeId))
            {
                return NotFound("Trustee not found");
            }

            if (createTrustRatingDto.PostId.HasValue && !await _postRepository.ExistAsync(createTrustRatingDto.PostId.Value))
            {
                return NotFound("Post not found");
            }

            TrustRating trustRating = createTrustRatingDto.ToTrustRatingFromCreateDto(userId);

            await _trustRatingRepository.CreateAsync(trustRating);

            return CreatedAtAction(nameof(GetById), new { id = trustRating.Id }, trustRating.ToTrustRatingDto());
        }

        /// <summary>
        /// Updates a trust rating with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the trust rating to update.</param>
        /// <param name="updateTrustRatingDto">The new trust rating data.</param>
        /// <returns>The updated trust rating, or a 401 Unauthorized if the user ID is not found, or a 404 Not Found if the trust rating does not exist.</returns>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(TrustRatingDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTrustRatingDto updateTrustRatingDto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _trustRatingRepository.UserOwnsTrustRatingAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the trust rating or the trust rating does not exist");
            }

            TrustRating? trustRating = await _trustRatingRepository.UpdateAsync(id, updateTrustRatingDto);

            if (trustRating == null)
            {
                return NotFound("Trust rating not found");
            }

            return Ok(trustRating.ToTrustRatingDto());
        }

        /// <summary>
        /// Deletes a trust rating with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the trust rating to delete.</param>
        /// <returns>A 200 OK response with the deleted trust rating, or a 401 Unauthorized if the user ID is not found or the user does not own the trust rating, or a 404 Not Found if the trust rating does not exist.</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(TrustRatingDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User ID not found");
            }

            if (!await _trustRatingRepository.UserOwnsTrustRatingAsync(id, userId))
            {
                return Unauthorized("User is not the owner of the trust rating or the trust rating does not exist");
            }

            TrustRating? trustRating = await _trustRatingRepository.DeleteAsync(id);

            if (trustRating == null)
            {
                return NotFound("Trust rating not found");
            }

            return Ok(trustRating.ToTrustRatingDto());
        }
    }
}