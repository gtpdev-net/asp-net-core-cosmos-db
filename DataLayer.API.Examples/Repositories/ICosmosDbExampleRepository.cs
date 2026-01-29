using DataLayer.API.Examples.Entities;

namespace DataLayer.API.Examples.Repositories;

/// <summary>
/// Repository interface for Example entity operations.
/// </summary>
public interface ICosmosDbExampleRepository : ICosmosDbRepository<CosmosDbExample>
{
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
}