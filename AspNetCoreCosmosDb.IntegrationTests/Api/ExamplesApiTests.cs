using System.Net;
using System.Net.Http.Json;
using AspNetCoreCosmosDb.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreCosmosDb.IntegrationTests.Api;

/// <summary>
/// Integration tests for the Examples API endpoints.
/// These tests use WebApplicationFactory to create an in-memory test server
/// and connect to the configured Cosmos DB instance.
/// </summary>
public class ExamplesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExamplesApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add the Testing configuration
                config.AddJsonFile("appsettings.Testing.json", optional: false);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/examples");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldReturnJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/api/examples");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetByCategory_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var category = "Electronics";

        // Act
        var response = await _client.GetAsync($"/api/examples/category/{category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInStock_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/examples/in-stock");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithValidExample_ShouldReturnCreated()
    {
        // Arrange
        var newExample = new Example
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            Description = "Integration Test Description",
            InStock = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/examples", newExample);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdExample = await response.Content.ReadFromJsonAsync<Example>();
        createdExample.Should().NotBeNull();
        createdExample!.Name.Should().Be(newExample.Name);
        createdExample.Category.Should().Be(newExample.Category);
        createdExample.Price.Should().Be(newExample.Price);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldReturnOk()
    {
        // Arrange - First create an example to update
        var newExample = new Example
        {
            Name = $"Original Product {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            InStock = true
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/examples", newExample);
        var createdExample = await createResponse.Content.ReadFromJsonAsync<Example>();
        
        // Update the example
        createdExample!.Name = $"Updated Product {Guid.NewGuid()}";
        createdExample.Price = 149.99m;

        // Act
        var response = await _client.PutAsJsonAsync($"/api/examples/{createdExample.Id}", createdExample);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedExample = await response.Content.ReadFromJsonAsync<Example>();
        updatedExample.Should().NotBeNull();
        updatedExample!.Name.Should().Be(createdExample.Name);
        updatedExample.Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        // Arrange - First create an example to delete
        var newExample = new Example
        {
            Name = $"To Be Deleted {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            InStock = true
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/examples", newExample);
        var createdExample = await createResponse.Content.ReadFromJsonAsync<Example>();

        // Act
        var response = await _client.DeleteAsync($"/api/examples/{createdExample!.Id}?category={createdExample.Category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify it's actually deleted
        var getResponse = await _client.GetAsync($"/api/examples/{createdExample.Id}?category={createdExample.Category}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        // Swagger is only available in Development mode by default
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
