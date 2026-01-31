using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using DataLayer.API.Examples.Configuration;

namespace DataLayer.API.Examples.IntegrationTests.TestBase;

/// <summary>
/// Shared fixture for integration tests that provides cleanup capabilities.
/// This is shared across all tests in a collection to enable batch cleanup.
/// </summary>
public class TestCollectionFixture : IAsyncLifetime
{
    public TestWebApplicationFactory Factory { get; private set; } = null!;
    private Container? _container;

    public async Task InitializeAsync()
    {
        Factory = new TestWebApplicationFactory();
        
        // Initialize the container reference for cleanup
        var scopeFactory = Factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        var config = scope.ServiceProvider.GetRequiredService<CosmosDbConfig>();
        _container = cosmosClient.GetContainer(config.DatabaseName, config.Containers.Examples);

        // Clean up any leftover test data from previous runs
        await CleanupTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up all test data after all tests in the collection complete
        await CleanupTestDataAsync();
        
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }

    /// <summary>
    /// Removes all test data from the database.
    /// This queries for items with category "IntegrationTest" and deletes them.
    /// </summary>
    private async Task CleanupTestDataAsync()
    {
        if (_container == null)
        {
            return;
        }

        try
        {
            // Query for all test data
            var query = new QueryDefinition("SELECT c.id, c.category FROM c WHERE c.category = @category")
                .WithParameter("@category", "IntegrationTest");
            
            var iterator = _container.GetItemQueryIterator<dynamic>(query);
            var itemsToDelete = new List<(string Id, string Category)>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    itemsToDelete.Add((item.id.ToString(), item.category.ToString()));
                }
            }

            // Delete all test items
            foreach (var (id, category) in itemsToDelete)
            {
                try
                {
                    await _container.DeleteItemAsync<dynamic>(id, new PartitionKey(category));
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Item already deleted, which is fine
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to delete test item {id}: {ex.Message}");
                }
            }

            if (itemsToDelete.Count > 0)
            {
                Console.WriteLine($"Cleaned up {itemsToDelete.Count} test items from the database.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to cleanup test data: {ex.Message}");
        }
    }
}

/// <summary>
/// Defines a test collection that shares the TestCollectionFixture.
/// All tests in this collection will share the same fixture instance.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class TestCollection : ICollectionFixture<TestCollectionFixture>
{
    // This class is just used to define the collection
}
