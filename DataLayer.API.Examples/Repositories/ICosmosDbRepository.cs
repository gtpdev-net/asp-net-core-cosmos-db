namespace DataLayer.API.Examples.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations on Cosmos DB entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ICosmosDbRepository<T> where T : class
{
    /// <summary>
    /// Retrieves an entity by its ID and partition key.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="partitionKey">The partition key value.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(string id, string partitionKey);

    /// <summary>
    /// Retrieves all entities from the container.
    /// </summary>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Executes a custom query against the container.
    /// </summary>
    /// <param name="queryString">The SQL query string.</param>
    /// <returns>A collection of entities matching the query.</returns>
    Task<IEnumerable<T>> QueryAsync(string queryString);

    /// <summary>
    /// Creates a new entity in the container.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="partitionKey">The partition key value.</param>
    /// <returns>The created entity.</returns>
    Task<T> CreateAsync(T entity, string partitionKey);

    /// <summary>
    /// Updates an existing entity in the container.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to update.</param>
    /// <param name="entity">The updated entity.</param>
    /// <param name="partitionKey">The partition key value.</param>
    /// <returns>The updated entity.</returns>
    Task<T> UpdateAsync(string id, T entity, string partitionKey);

    /// <summary>
    /// Deletes an entity from the container.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <param name="partitionKey">The partition key value.</param>
    Task DeleteAsync(string id, string partitionKey);
}
