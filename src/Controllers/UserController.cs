using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketTrustAPI.Data;
using MarketTrustAPI.Dtos.User;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Mappers;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketTrustAPI.Controllers
{
    /// <summary>
    /// Controller for managing users.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructs a new UserController.
        /// </summary>
        /// <param name="userRepository">The user repository.</param>
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Gets all users based on the filters.
        /// </summary>
        /// <param name="getUserDto">The filters for retrieving users.</param>
        /// <returns>A list of users matching the filters.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<UserDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetUserDto getUserDto)
        {
            List<User> users = await _userRepository.GetAllAsync(getUserDto);
            List<UserDto> userDtos = users
                .Select(user => user.ToUserDto())
                .ToList();

            return Ok(userDtos);
        }

        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The user with the specified ID, or a 404 Not Found if the user does not exist.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            User? user = await _userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user.ToUserDto());
        }

        /// <summary>
        /// Updates a user with the specified ID.
        /// </summary>
        /// <param name="updateUserDto">The new user data.</param>
        /// <returns>The updated user, or a 400 Bad Request if the user was not found.</returns>
        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto updateUserDto)
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id == null)
            {
                return Unauthorized("User ID not found");
            }

            User? user = await _userRepository.UpdateAsync(id, updateUserDto);

            if (user == null)
            {
                return BadRequest();
            }

            return Ok(user.ToUserDto());
        }

        /// <summary>
        /// Deletes a user with the specified ID.
        /// </summary>
        /// <returns>A 204 No Content response if the user was successfully deleted, a 401 Unauthorized if the user ID is not found, or a 404 Not Found if the user does not exist.</returns>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete()
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id == null)
            {
                return Unauthorized("User ID not found");
            }

            User? user = await _userRepository.DeleteAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}