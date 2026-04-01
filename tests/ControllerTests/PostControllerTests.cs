using System.Net;
using System.Net.Http.Json;
using MarketTrustAPI.Dtos.Post;
using MarketTrustAPI.Dtos.PropertyValue;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace ControllerTests;

public class PostControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public PostControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GetAll_ReturnsPostsWithGlobalTrust_WhenAnonymous()
	{
		List<Post> posts = [CreatePost(1, "u1")];
		_factory.PostRepository.GetAllAsync(Arg.Any<GetPostDto>()).Returns(posts);
		_factory.ReputationService.GetGlobalTrustAsync("u1").Returns(0.8);

		HttpResponseMessage response = await _client.GetAsync("/api/Post");
		List<PostDto>? result = await response.Content.ReadFromJsonAsync<List<PostDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Single(result);
		Assert.Equal(0.8, result[0].GlobalTrust);
		Assert.Null(result[0].PersonalTrust);
	}

	[Fact]
	public async Task GetAll_ReturnsPostsWithPersonalTrust_WhenAuthenticatedAndDProvided()
	{
		List<Post> posts = [CreatePost(1, "u2")];
		_factory.PostRepository.GetAllAsync(Arg.Any<GetPostDto>()).Returns(posts);
		_factory.ReputationService.GetGlobalTrustAsync("u2").Returns(0.8);
		_factory.ReputationService.GetPersonalTrustAsync("u1", "u2", 0.5).Returns(0.7);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/Post?D=0.5");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		List<PostDto>? result = await response.Content.ReadFromJsonAsync<List<PostDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Single(result);
		Assert.Equal(0.7, result[0].PersonalTrust);
	}

	[Fact]
	public async Task GetById_ReturnsPostWithTrust_WhenPostExists()
	{
		Post post = CreatePost(1, "u2");
		_factory.PostRepository.GetByIdAsync(1).Returns(post);
		_factory.ReputationService.GetGlobalTrustAsync("u2").Returns(0.8);
		_factory.ReputationService.GetPersonalTrustAsync("u1", "u2", 0.5).Returns(0.7);

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/Post/1?D=0.5");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
		Assert.Equal(0.8, result.GlobalTrust);
		Assert.Equal(0.7, result.PersonalTrust);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenPostDoesNotExist()
	{
		_factory.PostRepository.GetByIdAsync(0).Returns(Task.FromResult<Post?>(null));

		HttpResponseMessage response = await _client.GetAsync("/api/Post/1");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Create_ReturnsCreated_WhenValid()
	{
		_factory.UserRepository.ExistAsync("u1").Returns(true);
		_factory.CategoryRepository.ExistAsync(1).Returns(true);
		_factory.PostRepository.CreateAsync(Arg.Any<Post>()).Returns(Task.FromResult(new Post { Id = 1, UserId = "u1", CategoryId = 1 }));
		CreatePostDto dto = new() { Title = "title", CategoryId = 1 };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal("u1", result.UserId);
		Assert.Equal(1, result.CategoryId);
	}

	[Fact]
	public async Task Create_ReturnsUnauthorized_WhenNotAuthenticated()
	{
		CreatePostDto dto = new() { Title = "title", CategoryId = 1 };

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Post", dto);
        string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
	}

	[Fact]
	public async Task Create_ReturnsBadRequest_WhenUserDoesNotExist()
	{
		_factory.UserRepository.ExistAsync("u1").Returns(false);
		CreatePostDto dto = new() { Title = "title", CategoryId = 1 };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("User does not exist", content);
	}

	[Fact]
	public async Task Create_ReturnsBadRequest_WhenCategoryDoesNotExist()
	{
		_factory.UserRepository.ExistAsync("u1").Returns(true);
		_factory.CategoryRepository.ExistAsync(1).Returns(false);
		CreatePostDto dto = new() { Title = "title", CategoryId = 1 };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("Category does not exist", content);
	}

	[Fact]
	public async Task Create_ReturnsBadRequest_WhenPriceAndCurrencyMismatch()
	{
		_factory.UserRepository.ExistAsync("u1").Returns(true);
		_factory.CategoryRepository.ExistAsync(1).Returns(true);
		CreatePostDto dto = new() { Title = "title", CategoryId = 1, Price = 1 };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("Both Price and Currency must be provided together", content);
	}

	[Fact]
	public async Task Create_ReturnsBadRequest_WhenPriceNegative()
	{
		_factory.UserRepository.ExistAsync("u1").Returns(true);
		_factory.CategoryRepository.ExistAsync(1).Returns(true);
		CreatePostDto dto = new() { Title = "title", CategoryId = 1, Price = -1, Currency = Currency.USD };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("Price cannot be negative", content);
	}

    [Fact]
    public async Task Update_ReturnsOk_WhenValid()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        _factory.CategoryRepository.ExistAsync(1).Returns(true);
        Post updatedPost = CreatePost(1, "u1");
        updatedPost.Title = "new";
        _factory.PostRepository.UpdateAsync(1, Arg.Any<UpdatePostDto>()).Returns(Task.FromResult<Post?>(updatedPost));
        UpdatePostDto dto = new() { Title = "new" };

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("new", result.Title);
    }

    [Fact]
    public async Task Update_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        UpdatePostDto dto = new() { Title = "new" };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/Post/1", dto);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
        await _factory.PostRepository.DidNotReceive().UpdateAsync(Arg.Any<int>(), Arg.Any<UpdatePostDto>());
    }

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenUserIsNotOwner()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(false);
		UpdatePostDto dto = new() { Title = "new" };

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("User is not the owner of the post", content);
	}

	[Fact]
	public async Task Update_ReturnsBadRequest_WhenCategoryInvalid()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.CategoryRepository.ExistAsync(1).Returns(false);
		UpdatePostDto dto = new() { CategoryId = 1 };

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("Category does not exist", content);
	}

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenPriceNegative()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        UpdatePostDto dto = new() { Price = -1 };

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Price cannot be negative", content);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenCurrencyInvalid()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        UpdatePostDto dto = new() { Currency = (Currency)(-1) };

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid currency value", content);
    }

	[Fact]
	public async Task Update_ReturnsNotFound_WhenPostMissing()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.UpdateAsync(1, Arg.Any<UpdatePostDto>()).Returns(Task.FromResult<Post?>(null));
		UpdatePostDto dto = new() { Title = "new" };

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Post not found", content);
	}

    [Fact]
    public async Task Delete_ReturnsOk_WhenSuccessful()
    {
        Post post = CreatePost(1, "u1");
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        _factory.PostRepository.DeleteAsync(1).Returns(Task.FromResult<Post?>(post));

        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

        HttpResponseMessage response = await _client.SendAsync(request);
        PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        await _factory.PostRepository.Received(1).DeleteAsync(1);
    }

    [Fact]
    public async Task Delete_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/Post/1");
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
        await _factory.PostRepository.DidNotReceive().DeleteAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ReturnsUnauthorized_WhenUserIsNotOwner()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(false);

        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User is not the owner of the post", content);
        await _factory.PostRepository.DidNotReceive().DeleteAsync(Arg.Any<int>());
    }

	[Fact]
	public async Task Delete_ReturnsNotFound_WhenPostMissing()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.DeleteAsync(1).Returns(Task.FromResult<Post?>(null));

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Post not found", content);
	}

	[Fact]
	public async Task AddPropertyValue_ReturnsOk_WhenValid()
	{
		Post post = CreatePost(1, "u1");
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.PropertyNameExistsAsync(1, "property").Returns(false);
		_factory.PostRepository.AddPropertyValueAsync(1, Arg.Any<PropertyValue>()).Returns(post);
		AddPropertyValueDto dto = new() { Name = "property", Value = "value" };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
	}

    [Fact]
    public async Task AddPropertyValue_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        AddPropertyValueDto dto = new() { Name = "property", Value = "value" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Post/1", dto);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
        await _factory.PostRepository.DidNotReceive().AddPropertyValueAsync(Arg.Any<int>(), Arg.Any<PropertyValue>());
    }

    [Fact]
    public async Task AddPropertyValue_ReturnsUnauthorized_WhenUserIsNotOwner()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(false);
        AddPropertyValueDto dto = new() { Name = "property", Value = "value" };

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User is not the owner of the post", content);
        await _factory.PostRepository.DidNotReceive().AddPropertyValueAsync(Arg.Any<int>(), Arg.Any<PropertyValue>());
    }

	[Fact]
	public async Task AddPropertyValue_ReturnsBadRequest_WhenNameExists()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.PropertyNameExistsAsync(1, "Color").Returns(true);
		AddPropertyValueDto dto = new() { Name = "Color", Value = "Black" };

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("Property name already exists", content);
	}

    [Fact]
    public async Task AddPropertyValue_ReturnsNotFound_WhenPostMissing()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        _factory.PostRepository.AddPropertyValueAsync(1, Arg.Any<PropertyValue>()).Returns(Task.FromResult<Post?>(null));
        AddPropertyValueDto dto = new() { Name = "property", Value = "value" };

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/Post/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Post not found", content);
        await _factory.PostRepository.Received(1).AddPropertyValueAsync(1, Arg.Any<PropertyValue>());
    }

    [Fact]
    public async Task UpdatePropertyValue_ReturnsOk_WhenValid()
    {
        Post post = CreatePost(1, "u1");
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        UpdatePropertyValueDto dto = new() { Name = "updated_property", Value = "updated_value" };
        Post updatedPost = post;
        updatedPost.PropertyValues[0].Name = "updated_property";
        updatedPost.PropertyValues[0].Value = "updated_value";
        _factory.PostRepository.UpdatePropertyValueAsync(1, 1, Arg.Any<UpdatePropertyValueDto>()).Returns(updatedPost);

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("updated_property", result.PropertyValues[0].Name);
        Assert.Equal("updated_value", result.PropertyValues[0].Value);
    }

    [Fact]
    public async Task UpdatePropertyValue_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        UpdatePropertyValueDto dto = new() { Name = "updated_property", Value = "updated_value" };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/Post/1/1", dto);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
        await _factory.PostRepository.DidNotReceive().UpdatePropertyValueAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UpdatePropertyValueDto>());
    }

    [Fact]
    public async Task UpdatePropertyValue_ReturnsUnauthorized_WhenUserIsNotOwner()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(false);
        UpdatePropertyValueDto dto = new() { Name = "updated_property", Value = "updated_value" };

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
        request.Content = JsonContent.Create(dto);

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User is not the owner of the post", content);
        await _factory.PostRepository.DidNotReceive().UpdatePropertyValueAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<UpdatePropertyValueDto>());
    }

	[Fact]
	public async Task UpdatePropertyValue_ReturnsNotFound_WhenPostOrPropertyMissing()
	{
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.UpdatePropertyValueAsync(1, 1, Arg.Any<UpdatePropertyValueDto>()).Returns(Task.FromResult<Post?>(null));
		UpdatePropertyValueDto dto = new() { Name = "updated_property", Value = "updated_value" };

		using HttpRequestMessage request = new(HttpMethod.Put, "/api/Post/1/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");
		request.Content = JsonContent.Create(dto);

		HttpResponseMessage response = await _client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("Post or property not found", content);
	}

	[Fact]
	public async Task DeletePropertyValue_ReturnsOk_WhenValid()
	{
		Post post = CreatePost(1, "u1");
		_factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
		_factory.PostRepository.DeletePropertyValueAsync(1, 1).Returns(post);

		using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1/1");
		request.Headers.Add("Authorization", "Test");
		request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

		HttpResponseMessage response = await _client.SendAsync(request);
		PostDto? result = await response.Content.ReadFromJsonAsync<PostDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(1, result.Id);
	}

    [Fact]
    public async Task DeletePropertyValue_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/Post/1/1");
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User ID not found", content);
        await _factory.PostRepository.DidNotReceive().DeletePropertyValueAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePropertyValue_ReturnsUnauthorized_WhenUserIsNotOwner()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(false);

        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("User is not the owner of the post", content);
        await _factory.PostRepository.DidNotReceive().DeletePropertyValueAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePropertyValue_ReturnsNotFound_WhenPostOrPropertyMissing()
    {
        _factory.PostRepository.UserOwnsPostAsync(1, "u1").Returns(true);
        _factory.PostRepository.DeletePropertyValueAsync(1, 1).Returns(Task.FromResult<Post?>(null));

        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/Post/1/1");
        request.Headers.Add("Authorization", "Test");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, "u1");

        HttpResponseMessage response = await _client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Post or property not found", content);
    }

	private static Post CreatePost(int id, string userId)
	{
		return new Post
		{
			Id = id,
			Title = "Title",
			Content = "Content",
			CreatedAt = DateTime.UtcNow,
			UserId = userId,
			CategoryId = 1,
			Price = 1,
			Currency = Currency.USD,
			PropertyValues =
			[
				new PropertyValue { Id = 1, Name = "property", Value = "value", PostId = id }
			]
		};
	}
}