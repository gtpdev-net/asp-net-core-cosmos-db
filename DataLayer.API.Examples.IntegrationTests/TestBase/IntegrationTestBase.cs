using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Entities;

namespace DataLayer.API.Examples.IntegrationTests.TestBase;

/// <summary>
/// Base class for integration tests that provides automatic cleanup of test data.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    private readonly Container _container;
    private readonly List<(string Id, string PartitionKey)> _createdItems = new();

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        
        // Get the Cosmos DB container for cleanup operations
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        var config = scope.ServiceProvider.GetRequiredService<CosmosDbConfig>();
        _container = cosmosClient.GetContainer(config.DatabaseName, config.Containers.Examples);
    }

    /// <summary>
    /// Called before each test. Can be overridden in derived classes.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Called after each test to clean up any created data.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        // Clean up all items created during the test
        foreach (var (id, partitionKey) in _createdItems)
        {
            try
            {
                await _container.DeleteItemAsync<CosmosDbExample>(id, new PartitionKey(partitionKey));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Item already deleted, which is fine
            }
            catch (Exception ex)
            {
                // Log but don't fail the test cleanup
                Console.WriteLine($"Warning: Failed to delete item {id} in partition {partitionKey}: {ex.Message}");
            }
        }
        
        _createdItems.Clear();
    }

    /// <summary>
    /// Track an item that was created during the test for cleanup.
    /// Call this method whenever a test creates data in Cosmos DB.
    /// </summary>
    /// <param name="id">The ID of the created item.</param>
    /// <param name="partitionKey">The partition key of the created item.</param>
    protected void TrackCreatedItem(string id, string partitionKey)
    {
        _createdItems.Add((id, partitionKey));
    }

    /// <summary>
    /// Track an example entity that was created during the test for cleanup.
    /// </summary>
    /// <param name="example">The example entity to track.</param>
    protected void TrackCreatedItem(CosmosDbExample example)
    {
        if (example?.Id != null && example.Category != null)
        {
            TrackCreatedItem(example.Id, example.Category);
        }
    }
}
