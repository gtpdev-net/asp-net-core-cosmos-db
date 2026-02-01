using DataLayer.API.Examples.Entities;

namespace DataLayer.API.Examples.Services;

/// <summary>
/// Service interface for Example business logic operations.
/// </summary>
public interface ICosmosDbExampleService
{
    /// <summary>
    /// Retrieves all examples.
    /// </summary>
    /// <returns>A collection of all examples.</returns>
    Task<IEnumerable<CosmosDbExample>> GetAllExamplesAsync();

    /// <summary>
    /// Retrieves an example by its ID and category.
    /// </summary>
    /// <param name="id">The unique identifier of the example.</param>
    /// <param name="category">The category (partition key) of the example.</param>
    /// <returns>The example if found; otherwise, null.</returns>
    Task<CosmosDbExample?> GetExampleByIdAsync(string id, string category);

    /// <summary>
    /// Retrieves all examples in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A collection of examples in the specified category.</returns>
    Task<IEnumerable<CosmosDbExample>> GetExamplesByCategoryAsync(string category);

    /// <summary>
    /// Retrieves all examples that are currently in stock.
    /// </summary>
    /// <returns>A collection of in-stock examples.</returns>
    Task<IEnumerable<CosmosDbExample>> GetInStockExamplesAsync();

    /// <summary>
    /// Creates a new example.
    /// </summary>
    /// <param name="example">The example to create.</param>
    /// <returns>The created example.</returns>
    Task<CosmosDbExample> CreateExampleAsync(CosmosDbExample example);

    /// <summary>
    /// Updates an existing example.
    /// </summary>
    /// <param name="id">The unique identifier of the example to update.</param>
    /// <param name="example">The updated example data.</param>
    /// <returns>The updated example.</returns>
    Task<CosmosDbExample> UpdateExampleAsync(string id, CosmosDbExample example);

    /// <summary>
    /// Deletes an example.
    /// </summary>
    /// <param name="id">The unique identifier of the example to delete.</param>
    /// <param name="category">The category (partition key) of the example.</param>
    Task DeleteExampleAsync(string id, string category);
}
