using AspNetCoreCosmosDb.Entities;
using AspNetCoreCosmosDb.Repositories;

namespace AspNetCoreCosmosDb.Services;

/// <summary>
/// Service implementation for Example business logic operations.
/// </summary>
public class ExampleService : IExampleService
{
    private readonly IExampleRepository _exampleRepository;
    private readonly ILogger<ExampleService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleService"/> class.
    /// </summary>
    /// <param name="exampleRepository">The example repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ExampleService(
        IExampleRepository exampleRepository,
        ILogger<ExampleService> logger)
    {
        _exampleRepository = exampleRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Example>> GetAllExamplesAsync()
    {
        _logger.LogInformation("Retrieving all examples");
        return await _exampleRepository.GetAllAsync();
    }

    /// <inheritdoc />
    public async Task<Example?> GetExampleByIdAsync(string id, string category)
    {
        _logger.LogInformation("Retrieving example with id: {ExampleId}", id);
        return await _exampleRepository.GetByIdAsync(id, category);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Example>> GetExamplesByCategoryAsync(string category)
    {
        _logger.LogInformation("Retrieving examples in category: {Category}", category);
        return await _exampleRepository.GetExamplesByCategoryAsync(category);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Example>> GetInStockExamplesAsync()
    {
        _logger.LogInformation("Retrieving in-stock examples");
        return await _exampleRepository.GetInStockExamplesAsync();
    }

    /// <inheritdoc />
    public async Task<Example> CreateExampleAsync(Example example)
    {
        _logger.LogInformation("Creating new example: {ExampleName}", example.Name);
        
        // Business logic can go here (validation, enrichment, etc.)
        example.CreatedAt = DateTime.UtcNow;
        example.UpdatedAt = DateTime.UtcNow;
        
        return await _exampleRepository.CreateAsync(example, example.Category);
    }

    /// <inheritdoc />
    public async Task<Example> UpdateExampleAsync(string id, Example example)
    {
        _logger.LogInformation("Updating example with id: {ExampleId}", id);
        
        // Business logic can go here
        example.Id = id;
        example.UpdatedAt = DateTime.UtcNow;
        
        return await _exampleRepository.UpdateAsync(id, example, example.Category);
    }

    /// <inheritdoc />
    public async Task DeleteExampleAsync(string id, string category)
    {
        _logger.LogInformation("Deleting example with id: {ExampleId}", id);
        await _exampleRepository.DeleteAsync(id, category);
    }
}
