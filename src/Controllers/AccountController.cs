using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.Account;
using MarketTrustAPI.Dtos.User;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Models;
using MarketTrustAPI.SpatialIndexManager;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketTrustAPI.Controllers
{
    /// <summary>
    /// Controller for managing user authentication and registration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ISpatialIndexManager<User> _spatialIndexManager;

        /// <summary>
        /// Constructs a new AccountController.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="tokenService">The token service.</param>
        /// <param name="spatialIndexManager">The spatial index manager.</param>
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService, ISpatialIndexManager<User> spatialIndexManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _spatialIndexManager = spatialIndexManager;
        }

        /// <summary>
        /// Logs in a user with the provided credentials.
        /// </summary>
        /// <param name="loginDto">The login credentials.</param>
        /// <returns>A NewUserDto containing the user's information and token if login is successful, or an Unauthorized response if login fails.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(NewUserDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            User? user = await _userManager.FindByNameAsync(loginDto.Name);

            if (user == null)
            {
                return Unauthorized("Invalid username");
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (signInResult.Succeeded)
            {
                return Ok(
                    new NewUserDto
                    {
                        Id = user.Id,
                        Name = user.UserName,
                        Email = user.Email,
                        Token = _tokenService.CreateToken(user)
                    }
                );
            }
            else
            {
                return Unauthorized("Invalid password");
            }
        }

        /// <summary>
        /// Registers a new user with the provided information.
        /// </summary>
        /// <param name="registerDto">The registration information.</param>
        /// <returns>A NewUserDto containing the user's information and token if registration is successful, or a BadRequest response if registration fails.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(NewUserDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                User user = new()
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    IsPublicEmail = registerDto.IsPublicEmail,
                    PhoneNumber = registerDto.Phone,
                    IsPublicPhone = registerDto.IsPublicPhone,
                    Location = registerDto.Location,
                    IsPublicLocation = registerDto.IsPublicLocation
                };

                IdentityResult createResult = await _userManager.CreateAsync(user, registerDto.Password);

                if (createResult.Succeeded)
                {
                    IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "User");

                    if (roleResult.Succeeded)
                    {
                        _spatialIndexManager.Insert(user);

                        return Ok(
                            new NewUserDto
                            {
                                Id = user.Id,
                                Name = user.UserName,
                                Email = user.Email,
                                Token = _tokenService.CreateToken(user)
                            }
                        );
                    }
                    else
                    {
                        return BadRequest(roleResult.Errors);
                    }
                }
                else
                {
                    return BadRequest(createResult.Errors);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}