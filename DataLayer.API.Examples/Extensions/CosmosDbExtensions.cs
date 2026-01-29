using Azure.Identity;
using Microsoft.Azure.Cosmos;
using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Repositories;

namespace DataLayer.API.Examples.Extensions;

/// <summary>
/// Extension methods for configuring Cosmos DB services.
/// </summary>
public static class CosmosDbExtensions
{
    /// <summary>
    /// Adds Cosmos DB client and repositories to the service collection.
    /// Configures Cosmos DB with Managed Identity authentication and Gateway connection mode.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="cosmosDbConfig">The Cosmos DB configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCosmosDbPersistence(
        this IServiceCollection services,
        CosmosDbConfig cosmosDbConfig)
    {
        // Register CosmosClient as a singleton
        services.AddSingleton(_ =>
        {
            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            return new CosmosClient(
                cosmosDbConfig.Account,
                new DefaultAzureCredential(),
                cosmosClientOptions);
        });

        // Register repositories
        services.AddScoped<ICosmosDbExampleRepository, CosmosDbExampleRepository>();
        // Add more repositories here as needed:
        // services.AddScoped<IOrderRepository, OrderRepository>();
        // services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
