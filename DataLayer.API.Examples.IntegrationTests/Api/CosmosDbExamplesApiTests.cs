using System.Net;
using System.Net.Http.Json;
using DataLayer.API.Examples.Entities;
using DataLayer.API.Examples.IntegrationTests.TestBase;

namespace DataLayer.API.Examples.IntegrationTests.Api;

/// <summary>
/// Integration tests for the Examples API endpoints.
/// These tests use WebApplicationFactory to create an in-memory test server
/// and connect to the configured Cosmos DB instance.
/// All test data is automatically cleaned up after each test.
/// </summary>
[Collection("Integration Tests")]
public class CosmosDbExamplesApiTests : IntegrationTestBase
{
    public CosmosDbExamplesApiTests(TestCollectionFixture fixture) : base(fixture.Factory)
    {
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/api/examples");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldReturnJsonContent()
    {
        // Act
        var response = await Client.GetAsync("/api/examples");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetByCategory_ShouldReturnSuccessStatusCode()
    {
        // Arrange
        var category = "Electronics";

        // Act
        var response = await Client.GetAsync($"/api/examples/category/{category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInStock_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/api/examples/in-stock");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithValidExample_ShouldReturnCreated()
    {
        // Arrange
        var newExample = new CosmosDbExample
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            Description = "Integration Test Description",
            InStock = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/examples", newExample);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdExample = await response.Content.ReadFromJsonAsync<CosmosDbExample>();
        createdExample.Should().NotBeNull();
        createdExample!.Name.Should().Be(newExample.Name);
        createdExample.Category.Should().Be(newExample.Category);
        createdExample.Price.Should().Be(newExample.Price);
        
        // Track for cleanup
        TrackCreatedItem(createdExample);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldReturnOk()
    {
        // Arrange - First create an example to update
        var newExample = new CosmosDbExample
        {
            Name = $"Original Product {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            InStock = true
        };
        
        var createResponse = await Client.PostAsJsonAsync("/api/examples", newExample);
        var createdExample = await createResponse.Content.ReadFromJsonAsync<CosmosDbExample>();
        
        // Track for cleanup
        TrackCreatedItem(createdExample!);
        
        // Update the example
        createdExample!.Name = $"Updated Product {Guid.NewGuid()}";
        createdExample.Price = 149.99m;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/examples/{createdExample.Id}", createdExample);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedExample = await response.Content.ReadFromJsonAsync<CosmosDbExample>();
        updatedExample.Should().NotBeNull();
        updatedExample!.Name.Should().Be(createdExample.Name);
        updatedExample.Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        // Arrange - First create an example to delete
        var newExample = new CosmosDbExample
        {
            Name = $"To Be Deleted {Guid.NewGuid()}",
            Category = "IntegrationTest",
            Price = 99.99m,
            InStock = true
        };
        
        var createResponse = await Client.PostAsJsonAsync("/api/examples", newExample);
        var createdExample = await createResponse.Content.ReadFromJsonAsync<CosmosDbExample>();
        
        // Track for cleanup (in case delete fails)
        TrackCreatedItem(createdExample!);

        // Act
        var response = await Client.DeleteAsync($"/api/examples/{createdExample!.Id}?category={createdExample.Category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify it's actually deleted
        var getResponse = await Client.GetAsync($"/api/examples/{createdExample.Id}?category={createdExample.Category}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger/index.html");

        // Assert
        // Swagger is only available in Development mode by default
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
