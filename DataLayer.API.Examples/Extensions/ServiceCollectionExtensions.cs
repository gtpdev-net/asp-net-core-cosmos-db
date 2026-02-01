using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Services;

namespace DataLayer.API.Examples.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services including Cosmos DB and application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="cosmosDbConfig">The Cosmos DB configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureExtensions(
        this IServiceCollection services,
        IConfiguration configuration,
        CosmosDbConfig cosmosDbConfig)
    {
        // Register configuration as singleton
        services.AddSingleton(cosmosDbConfig);
        
        CosmosDbExtensions.AddCosmosDbPersistence(services, cosmosDbConfig);
        
        // Register services
        services.AddScoped<ICosmosDbExampleService, CosmosDbExampleService>();
        
        return services;
    }
}
