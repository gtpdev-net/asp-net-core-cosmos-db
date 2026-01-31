namespace DataLayer.API.Examples.Configuration;

/// <summary>
/// Configuration settings for Azure Cosmos DB.
/// </summary>
public class CosmosDbConfig
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Gets or sets the Cosmos DB account endpoint URI.
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the Cosmos DB database.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name configurations.
    /// </summary>
    public ContainersConfig Containers { get; set; } = new();

    /// <summary>
    /// Configuration for Cosmos DB container names.
    /// </summary>
    public class ContainersConfig
    {
        /// <summary>
        /// Gets or sets the name of the Examples container.
        /// </summary>
        public string Examples { get; set; } = string.Empty;
    }
}
