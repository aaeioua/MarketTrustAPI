using System.Net;
using System.Net.Http.Json;
using MarketTrustAPI.Dtos.Category;
using MarketTrustAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace ControllerTests;

public class CategoryControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory<Program> _factory;

	public CategoryControllerTests(CustomWebApplicationFactory<Program> factory)
	{
		_factory = factory;
		_factory.ResetSubstitutes();
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GetAll_ReturnsCategoriesWithInheritedProperties()
	{
		List<Category> categories =
		[
			new Category
			{
				Id = 1,
				Name = "category1",
				ParentId = null,
				Properties = new List<Property>
				{
					new Property { Id = 1, Name = "property1", IsMandatory = true, CategoryId = 1 }
				}
			},
			new Category
			{
				Id = 2,
				Name = "category2",
				ParentId = 1,
				Properties = new List<Property>
				{
					new Property { Id = 2, Name = "property2", IsMandatory = false, CategoryId = 2 }
				}
			}
		];

		_factory.CategoryRepository.GetAllAsync(Arg.Any<GetCategoryDto>()).Returns(categories);
		_factory.CategoryRepository.GetInheritedPropertiesAsync(1).Returns(new List<Property>());
		_factory.CategoryRepository.GetInheritedPropertiesAsync(2).Returns(new List<Property>
		{
			new Property { Id = 1, Name = "property1", IsMandatory = true, CategoryId = 1 }
		});

		HttpResponseMessage response = await _client.GetAsync("/api/Category");
		List<CategoryDto>? result = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);

		CategoryDto root = result.Single(x => x.Id == 1);
		Assert.Single(root.Properties);
		Assert.Empty(root.InheritedProperties);

		CategoryDto child = result.Single(x => x.Id == 2);
		Assert.Single(child.Properties);
		Assert.Single(child.InheritedProperties);
		Assert.Equal("property1", child.InheritedProperties[0].Name);

		await _factory.CategoryRepository.Received(1).GetInheritedPropertiesAsync(1);
		await _factory.CategoryRepository.Received(1).GetInheritedPropertiesAsync(2);
	}

	[Fact]
	public async Task GetById_ReturnsCategoryWithInheritedProperties_WhenCategoryExists()
	{
		Category category = new()
		{
			Id = 2,
			Name = "category2",
			ParentId = 1,
			Properties = new List<Property>
			{
				new Property { Id = 2, Name = "property2", IsMandatory = false, CategoryId = 2 }
			}
		};

		_factory.CategoryRepository.GetByIdAsync(2).Returns(category);
		_factory.CategoryRepository.GetInheritedPropertiesAsync(2).Returns(new List<Property>
		{
			new Property { Id = 1, Name = "property1", IsMandatory = true, CategoryId = 1 }
		});

		HttpResponseMessage response = await _client.GetAsync("/api/Category/2");
		CategoryDto? result = await response.Content.ReadFromJsonAsync<CategoryDto>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(result);
		Assert.Equal(2, result.Id);
		Assert.Equal("category2", result.Name);
		Assert.Single(result.Properties);
		Assert.Single(result.InheritedProperties);
		Assert.Equal("property1", result.InheritedProperties[0].Name);

		await _factory.CategoryRepository.Received(1).GetInheritedPropertiesAsync(2);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenCategoryDoesNotExist()
	{
		_factory.CategoryRepository.GetByIdAsync(1).Returns(Task.FromResult<Category?>(null));

		HttpResponseMessage response = await _client.GetAsync("/api/Category/1");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}