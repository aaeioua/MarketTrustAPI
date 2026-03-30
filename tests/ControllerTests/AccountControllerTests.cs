using System.Net;
using System.Net.Http.Json;
using MarketTrustAPI.Dtos.User;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace ControllerTests;

public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public AccountControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task Login_ReturnsUserWithToken_WhenCredentialsAreValid()
	{
		User user = new() { Id = "user-id", UserName = "username", Email = "username@example.com" };
		_factory.UserManager.FindByNameAsync("username").Returns(user);
		_factory.SignInManager.CheckPasswordSignInAsync(user, "password", false).Returns(SignInResult.Success);
		_factory.TokenService.CreateToken(user).Returns("test-jwt-token");

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/login", new { name = "username", password = "password" });
		NewUserDto? dto = await response.Content.ReadFromJsonAsync<NewUserDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(dto);
		Assert.Equal("user-id", dto.Id);
		Assert.Equal("username", dto.Name);
		Assert.Equal("username@example.com", dto.Email);
		Assert.Equal("test-jwt-token", dto.Token);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
	{
		_factory.UserManager.FindByNameAsync("missing").Returns(Task.FromResult<User?>(null));

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/login", new { name = "missing", password = "password" });
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("Invalid username", content);
		await _factory.SignInManager.DidNotReceive().CheckPasswordSignInAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<bool>());
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
	{
		User user = new() { Id = "user-id", UserName = "username", Email = "username@example.com" };
		_factory.UserManager.FindByNameAsync("username").Returns(user);
		_factory.SignInManager.CheckPasswordSignInAsync(user, "password", false).Returns(SignInResult.Failed);

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/login", new { name = "username", password = "password" });
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("Invalid password", content);
	}

	[Fact]
	public async Task Register_ReturnsUserWithTokenAndInsertsUser_WhenSuccessful()
	{
		_factory.UserManager.CreateAsync(Arg.Any<User>(), "password").Returns(IdentityResult.Success);
		_factory.UserManager.AddToRoleAsync(Arg.Any<User>(), "User").Returns(IdentityResult.Success);
		_factory.TokenService.CreateToken(Arg.Any<User>()).Returns("test-jwt-token");

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/register", new
		{
			userName = "username",
			email = "user@example.com",
			isPublicEmail = true,
			phone = "+4512345678",
			isPublicPhone = true,
			isPublicLocation = true,
			password = "password"
		});
		NewUserDto? dto = await response.Content.ReadFromJsonAsync<NewUserDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		NewUserDto result = Assert.IsType<NewUserDto>(dto);
        Assert.NotNull(result.Id);
		Assert.Equal("username", result.Name);
		Assert.Equal("user@example.com", result.Email);
		Assert.Equal("test-jwt-token", result.Token);

		_factory.SpatialIndexManager.Received(1).Insert(Arg.Any<User>());
	}

	[Fact]
	public async Task Register_ReturnsBadRequest_WhenCreateFails()
	{
		IdentityResult createResult = IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "Username already exists." });
		_factory.UserManager.CreateAsync(Arg.Any<User>(), "password").Returns(createResult);

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/register", new
		{
			userName = "username",
			email = "user@example.com",
			password = "password"
		});
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Contains("Username already exists.", content);
		_factory.SpatialIndexManager.DidNotReceive().Insert(Arg.Any<User>());
	}

	[Fact]
	public async Task Register_ReturnsBadRequest_WhenAddToRoleFails()
    {
        _factory.UserManager.CreateAsync(Arg.Any<User>(), "password").Returns(IdentityResult.Success);
        IdentityResult roleResult = IdentityResult.Failed(new IdentityError { Code = "RoleNotFound", Description = "Role 'User' not found." });
        _factory.UserManager.AddToRoleAsync(Arg.Any<User>(), "User").Returns(roleResult);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Account/register", new
        {
            userName = "username",
            email = "user@example.com",
            password = "password"
        });
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Role 'User' not found.", content);
        _factory.SpatialIndexManager.DidNotReceive().Insert(Arg.Any<User>());
    }
}
