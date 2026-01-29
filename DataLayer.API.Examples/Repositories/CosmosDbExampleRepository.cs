using Microsoft.Azure.Cosmos;
using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Entities;

namespace DataLayer.API.Examples.Repositories;

/// <summary>
/// Repository implementation for Example entity operations in Cosmos DB.
/// </summary>
public class CosmosDbExampleRepository : CosmosRepositoryBase<CosmosDbExample>, ICosmosDbExampleRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbExampleRepository"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="cosmosDbConfig">The Cosmos DB configuration.</param>
    public CosmosDbExampleRepository(CosmosClient cosmosClient, CosmosDbConfig cosmosDbConfig) 
        : base(
            cosmosClient, 
            cosmosDbConfig.DatabaseName, 
            cosmosDbConfig.Containers.Examples)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CosmosDbExample>> GetExamplesByCategoryAsync(string category)
    {
        var query = $"SELECT * FROM c WHERE c.category = '{category}'";
        return await QueryAsync(query);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CosmosDbExample>> GetInStockExamplesAsync()
    {
        var query = "SELECT * FROM c WHERE c.inStock = true";
        return await QueryAsync(query);
    }
}
