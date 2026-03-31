using System.Net;
using System.Net.Http.Json;
using MarketTrustAPI.Dtos.User;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace ControllerTests;

public class UserControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public UserControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GetAll_ReturnsMappedUsers()
	{
		List<User> users =
		[
			new User
			{
				Id = "u1",
				UserName = "user1",
				Email = "user1@example.com",
				IsPublicEmail = true,
                PhoneNumber = "+4511111111",
                IsPublicPhone = true
			},
			new User
			{
				Id = "u2",
				UserName = "user2",
				Email = "user2@example.com",
				IsPublicEmail = false,
                PhoneNumber = "+4522222222",
                IsPublicPhone = false
			}
		];

		_factory.UserRepository.GetAllAsync(Arg.Any<GetUserDto>()).Returns(users);

		HttpResponseMessage response = await _client.GetAsync("/api/User?name=user");
		List<UserDto>? dtos = await response.Content.ReadFromJsonAsync<List<UserDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(dtos);
		Assert.Equal(2, dtos.Count);
		Assert.Equal("user1", dtos[0].Name);
		Assert.Equal("user1@example.com", dtos[0].Email);
        Assert.Equal("+4511111111", dtos[0].Phone);
		Assert.Equal("user2", dtos[1].Name);
		Assert.Null(dtos[1].Email);
        Assert.Null(dtos[1].Phone);
        await _factory.UserRepository.Received(1).GetAllAsync(Arg.Is<GetUserDto>(dto => dto!.Name == "user" && dto.Email == null && dto.Phone == null));
	}

	[Fact]
	public async Task GetById_ReturnsUser_WhenRepositoryReturnsUser()
	{
		User user = new()
		{
			Id = "id",
			UserName = "username",

		};

		_factory.UserRepository.GetByIdAsync("id").Returns(user);

		HttpResponseMessage response = await _client.GetAsync("/api/User/id");
		UserDto? dto = await response.Content.ReadFromJsonAsync<UserDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(dto);
		Assert.Equal("id", dto.Id);
		Assert.Equal("username", dto.Name);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenRepositoryReturnsNull()
	{
		_factory.UserRepository.GetByIdAsync("missing").Returns(Task.FromResult<User?>(null));

		HttpResponseMessage response = await _client.GetAsync("/api/User/missing");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Update_ReturnsUpdatedUser_WhenSuccessful()
	{
		User updatedUser = new()
		{
			Id = "u1",
			UserName = "username-updated",
			Email = "updatedEmail@example.com",
			IsPublicEmail = true
		};

		_factory.UserRepository.UpdateAsync("u1", Arg.Any<UpdateUserDto>()).Returns(updatedUser);

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/User")
		{
			Content = JsonContent.Create(new { name = "username-updated", email = "updatedEmail@example.com", isPublicEmail = true })
		};
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		UserDto? dto = await response.Content.ReadFromJsonAsync<UserDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(dto);
		Assert.Equal("u1", dto.Id);
		Assert.Equal("username-updated", dto.Name);
		Assert.Equal("updatedEmail@example.com", dto.Email);
	}

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.PutAsJsonAsync("/api/User", new { name = "username-updated" });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		await _factory.UserRepository.DidNotReceive().UpdateAsync(Arg.Any<string>(), Arg.Any<UpdateUserDto>());
	}

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenNameIdentifierClaimIsMissing()
	{
		using HttpRequestMessage request = new(HttpMethod.Put, "/api/User")
		{
			Content = JsonContent.Create(new { name = "username-updated" })
		};
		request.Headers.Add("Authorization", "Test");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task Update_ReturnsBadRequest_WhenRepositoryReturnsNull()
	{
		_factory.UserRepository.UpdateAsync("u1", Arg.Any<UpdateUserDto>()).Returns(Task.FromResult<User?>(null));

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/User")
		{
			Content = JsonContent.Create(new { name = "username-updated" })
		};
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Delete_ReturnsNoContent_WhenSuccessful()
	{
        User deletedUser = new() { Id = "u1", UserName = "username" };
		_factory.UserRepository.DeleteAsync("u1").Returns(deletedUser);

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/User");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
		await _factory.UserRepository.Received(1).DeleteAsync("u1");
	}

	[Fact]
	public async Task Delete_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.DeleteAsync("/api/User");
        string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
		await _factory.UserRepository.DidNotReceive().DeleteAsync(Arg.Any<string>());
	}

	[Fact]
	public async Task Delete_ReturnsNotFound_WhenRepositoryReturnsNull()
	{
		_factory.UserRepository.DeleteAsync("u1").Returns(Task.FromResult<User?>(null));

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/User");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}