using Microsoft.Azure.Cosmos;

namespace DataLayer.API.Examples.Repositories;

/// <summary>
/// Base repository implementation for Cosmos DB operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class CosmosRepositoryBase<T> : ICosmosDbRepository<T> where T : class
{
    /// <summary>
    /// The Cosmos DB container instance.
    /// Additional query methods are available on the Container object.
    /// See <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.container?view=azure-dotnet">Container API documentation</see> for more information.
    /// </summary>
    protected readonly Container Container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosRepositoryBase{T}"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    /// <param name="databaseName">The name of the database.</param>
    /// <param name="containerName">The name of the container.</param>
    protected CosmosRepositoryBase(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        Container = cosmosClient.GetContainer(databaseName, containerName);
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(string id, string partitionKey)
    {
        try
        {
            var response = await Container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var query = Container.GetItemQueryIterator<T>(new QueryDefinition("SELECT * FROM c"));
        var results = new List<T>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> QueryAsync(string queryString)
    {
        var query = Container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
        var results = new List<T>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    /// <inheritdoc />
    public virtual async Task<T> CreateAsync(T entity, string partitionKey)
    {
        var response = await Container.CreateItemAsync(entity, new PartitionKey(partitionKey));
        return response.Resource;
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(string id, T entity, string partitionKey)
    {
        var response = await Container.UpsertItemAsync(entity, new PartitionKey(partitionKey));
        return response.Resource;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(string id, string partitionKey)
    {
        await Container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
    }
}
