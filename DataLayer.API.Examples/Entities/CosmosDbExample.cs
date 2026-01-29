namespace DataLayer.API.Examples.Entities;

/// <summary>
/// Represents an example entity stored in Cosmos DB.
/// </summary>
public class CosmosDbExample
{
    /// <summary>
    /// Gets or sets the unique identifier for the example.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the example.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the example. Used as the partition key.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price of the example.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the optional description of the example.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the example is in stock.
    /// </summary>
    public bool InStock { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the example was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC date and time when the example was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
