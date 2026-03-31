using System.Net;
using System.Net.Http.Json;
using MarketTrustAPI.Dtos.TrustRating;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace ControllerTests;

public class TrustRatingControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public TrustRatingControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GetAll_ReturnsList_WhenTrustRatingsExist()
	{
		var trustRatings = new List<TrustRating>
		{
			new TrustRating { Id = 1, TrustorId = "u1", TrusteeId = "u2", TrustValue = 0.7, Comment = "Test" },
			new TrustRating { Id = 2, TrustorId = "u1", TrusteeId = "u3", TrustValue = 0.8, Comment = "Test" }
		};
		_factory.TrustRatingRepository.GetAllAsync(Arg.Any<GetTrustRatingDto>(), "u1").Returns(trustRatings);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/TrustRating");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		List<TrustRatingDto>? ratings = await response.Content.ReadFromJsonAsync<List<TrustRatingDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(ratings);
		Assert.Equal(2, ratings.Count);
	}

	[Fact]
	public async Task GetAll_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.GetAsync("/api/TrustRating");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task GetAll_ReturnsUnauthorized_WhenNameIdentifierClaimIsMissing()
	{
		using HttpRequestMessage request = new(HttpMethod.Get, "/api/TrustRating");
		request.Headers.Add("Authorization", "Test");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task GetById_ReturnsTrustRating_WhenUserIsOwner()
	{
		TrustRating trustRating = new TrustRating { Id = 1, TrustorId = "u1", TrusteeId = "u2", TrustValue = 0.85, Comment = "Test" };
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
		_factory.TrustRatingRepository.GetByIdAsync(1).Returns(trustRating);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		TrustRatingDto? rating = await response.Content.ReadFromJsonAsync<TrustRatingDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(rating);
		Assert.Equal(1, rating.Id);
	}

	[Fact]
	public async Task GetById_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.GetAsync("/api/TrustRating/1");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task GetById_ReturnsUnauthorized_WhenUserIsNotOwner()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(false);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User is not the owner of the trust rating or the trust rating does not exist", content);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenTrustRatingDoesNotExist()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
		_factory.TrustRatingRepository.GetByIdAsync(1).Returns(Task.FromResult<TrustRating?>(null));

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Create_ReturnsTrustRating_WhenValid()
	{
		_factory.UserRepository.ExistAsync("u2").Returns(true);
		TrustRating trustRating = new TrustRating { Id = 1, TrustorId = "u1", TrusteeId = "u2", TrustValue = 0.8, Comment = "Test" };
		_factory.TrustRatingRepository.CreateAsync(Arg.Any<TrustRating>()).Returns(Task.FromResult(trustRating));

		CreateTrustRatingDto createDto = new CreateTrustRatingDto { TrusteeId = "u2", TrustValue = 0.8, Comment = "Test" };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/TrustRating");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(createDto);

		HttpResponseMessage response = await _client.SendAsync(request);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact]
	public async Task Create_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		CreateTrustRatingDto createDto = new CreateTrustRatingDto { TrusteeId = "u2", TrustValue = 0.8, Comment = "Test" };

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/TrustRating", createDto);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Create_ReturnsBadRequest_WhenRatingSelf()
	{
		CreateTrustRatingDto createDto = new CreateTrustRatingDto { TrusteeId = "u1", TrustValue = 0.8, Comment = "Test" };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/TrustRating");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(createDto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("User cannot rate self", content);
	}

	[Fact]
	public async Task Create_ReturnsNotFound_WhenTrusteeDoesNotExist()
	{
		_factory.UserRepository.ExistAsync("u2").Returns(false);
		CreateTrustRatingDto createDto = new CreateTrustRatingDto { TrusteeId = "u2", TrustValue = 0.8, Comment = "Test" };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/TrustRating");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(createDto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Trustee not found", content);
	}

	[Fact]
	public async Task Update_ReturnsTrustRating_WhenValid()
	{
		TrustRating updatedRating = new TrustRating { Id = 1, TrustorId = "u1", TrusteeId = "u2", TrustValue = 0.9, Comment = "Test" };
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
		UpdateTrustRatingDto updateDto = new UpdateTrustRatingDto { TrustValue = 0.9 };
		_factory.TrustRatingRepository.UpdateAsync(1, Arg.Any<UpdateTrustRatingDto>()).Returns(Task.FromResult<TrustRating?>(updatedRating));

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(updateDto);

		HttpResponseMessage response = await _client.SendAsync(request);
        TrustRatingDto? result = await response.Content.ReadFromJsonAsync<TrustRatingDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
        Assert.Equal(0.9, result.TrustValue);
	}

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		UpdateTrustRatingDto updateDto = new UpdateTrustRatingDto { TrustValue = 0.9 };

		HttpResponseMessage response = await _client.PutAsJsonAsync("/api/TrustRating/1", updateDto);
        string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenUserIsNotOwner()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(false);
		UpdateTrustRatingDto updateDto = new UpdateTrustRatingDto { TrustValue = 0.9 };

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(updateDto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User is not the owner of the trust rating or the trust rating does not exist", content);
	}

	[Fact]
	public async Task Update_ReturnsNotFound_WhenTrustRatingDoesNotExist()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
        UpdateTrustRatingDto updateDto = new UpdateTrustRatingDto { TrustValue = 0.9 };
		_factory.TrustRatingRepository.UpdateAsync(1, updateDto).Returns(Task.FromResult<TrustRating?>(null));

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(updateDto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Trust rating not found", content);
	}

	[Fact]
	public async Task Delete_ReturnsTrustRating_WhenValid()
	{
		TrustRating deletedRating = new TrustRating { Id = 1, TrustorId = "u1", TrusteeId = "u2", TrustValue = 0.8, Comment = "Test" };
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
		_factory.TrustRatingRepository.DeleteAsync(1).Returns(Task.FromResult<TrustRating?>(deletedRating));

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		TrustRatingDto? result = await response.Content.ReadFromJsonAsync<TrustRatingDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task Delete_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.DeleteAsync("/api/TrustRating/1");
        string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task Delete_ReturnsUnauthorized_WhenUserIsNotOwner()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(false);

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User is not the owner of the trust rating or the trust rating does not exist", content);
	}

	[Fact]
	public async Task Delete_ReturnsNotFound_WhenTrustRatingDoesNotExist()
	{
		_factory.TrustRatingRepository.UserOwnsTrustRatingAsync(1, "u1").Returns(true);
		_factory.TrustRatingRepository.DeleteAsync(1).Returns(Task.FromResult<TrustRating?>(null));

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/TrustRating/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Trust rating not found", content);
	}
}