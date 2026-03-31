using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace ControllerTests;

public class ReputationControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public ReputationControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GetGlobalTrust_ReturnsValue_WhenServiceReturnsValue()
	{
		_factory.ReputationService.GetGlobalTrustAsync("u1").Returns(0.85);

		HttpResponseMessage response = await _client.GetAsync("/api/Reputation/global/u1");
		double? value = await response.Content.ReadFromJsonAsync<double?>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(value);
		Assert.Equal(0.85, value);
		await _factory.ReputationService.Received(1).GetGlobalTrustAsync("u1");
	}

	[Fact]
	public async Task GetGlobalTrust_ReturnsNotFound_WhenServiceReturnsNull()
	{
		_factory.ReputationService.GetGlobalTrustAsync("missing").Returns(Task.FromResult<double?>(null));

		HttpResponseMessage response = await _client.GetAsync("/api/Reputation/global/missing");
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("User not found", content);
	}

	[Fact]
	public async Task GetPersonalTrust_ReturnsValue_WhenAuthorizedAndServiceReturnsValue()
	{
		_factory.ReputationService.GetPersonalTrustAsync("u1", "u2", 0.5).Returns(0.85);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/Reputation/personal?TrusteeId=u2&D=0.5");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		double? value = await response.Content.ReadFromJsonAsync<double?>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(value);
		Assert.Equal(0.85, value);
		await _factory.ReputationService.Received(1).GetPersonalTrustAsync("u1", "u2", 0.5);
	}

	[Fact]
	public async Task GetPersonalTrust_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		HttpResponseMessage response = await _client.GetAsync("/api/Reputation/personal?TrusteeId=u1&D=0.5");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		await _factory.ReputationService.DidNotReceive().GetPersonalTrustAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>());
	}

	[Fact]
	public async Task GetPersonalTrust_ReturnsUnauthorized_WhenNameIdentifierClaimIsMissing()
	{
		using HttpRequestMessage request = new(HttpMethod.Get, "/api/Reputation/personal?TrusteeId=u2&D=0.5");
		request.Headers.Add("Authorization", "Test");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task GetPersonalTrust_ReturnsNotFound_WhenServiceReturnsNull()
	{
		_factory.ReputationService.GetPersonalTrustAsync("u1", "u2", 0.5).Returns(Task.FromResult<double?>(null));

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/Reputation/personal?TrusteeId=u2&D=0.5");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("User not found", content);
	}
}