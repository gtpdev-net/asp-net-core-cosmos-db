using Microsoft.Azure.Cosmos;
using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Entities;

namespace DataLayer.API.Examples.Repositories;

/// <summary>
/// Repository interface for Example entity operations.
/// </summary>
public interface IExampleRepository : IRepository<Example>
{
    /// <summary>
    /// Retrieves all examples in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A collection of examples in the specified category.</returns>
    Task<IEnumerable<Example>> GetExamplesByCategoryAsync(string category);

    /// <summary>
    /// Retrieves all examples that are currently in stock.
    /// </summary>
    /// <returns>A collection of in-stock examples.</returns>
    Task<IEnumerable<Example>> GetInStockExamplesAsync();
}

/// <summary>
/// Repository implementation for Example entity operations in Cosmos DB.
/// </summary>
public class ExampleRepository : CosmosRepositoryBase<Example>, IExampleRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleRepository"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="cosmosDbConfig">The Cosmos DB configuration.</param>
    public ExampleRepository(CosmosClient cosmosClient, CosmosDbConfig cosmosDbConfig) 
        : base(
            cosmosClient, 
            cosmosDbConfig.DatabaseName, 
            cosmosDbConfig.Containers.Examples)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Example>> GetExamplesByCategoryAsync(string category)
    {
        var query = $"SELECT * FROM c WHERE c.category = '{category}'";
        return await QueryAsync(query);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Example>> GetInStockExamplesAsync()
    {
        var query = "SELECT * FROM c WHERE c.inStock = true";
        return await QueryAsync(query);
    }
}
