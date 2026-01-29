# ASP.NET Core DataLayer API

A production-ready ASP.NET Core 8.0 Web API template demonstrating best practices for integrating Azure Cosmos DB with clean architecture, repository pattern, and comprehensive testing.

## Features

- **ASP.NET Core 8.0** Web API with Swagger/OpenAPI documentation
- **Azure Cosmos DB** integration with NoSQL API
- **Managed Identity Authentication** using `DefaultAzureCredential`
- **Repository Pattern** with generic base repository implementation
- **Service Layer** for business logic separation
- **Clean Architecture** with proper separation of concerns
- **Comprehensive Testing** with unit and integration tests
- **Dependency Injection** configured for all services and repositories
- **Camel Case JSON Serialization** for API responses
- **Lowercase URL Routing** for consistent endpoints

## Project Structure

```
├── DataLayer.API.Examples/              # Main API project
│   ├── Configuration/               # Configuration models
│   ├── Controllers/                 # API controllers
│   ├── Entities/                    # Domain entities
│   ├── Extensions/                  # Service registration extensions
│   ├── Repositories/                # Data access layer
│   └── Services/                    # Business logic layer
├── DataLayer.API.Examples.UnitTests/    # Unit tests for services and controllers
└── DataLayer.API.Examples.IntegrationTests/  # API integration tests
```

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Azure Cosmos DB account with NoSQL API
- Azure CLI (for authentication)

### Configuration

1. **Update Cosmos DB Settings** in `appsettings.json`:

```json
{
  "CosmosDb": {
    "Account": "https://your-account.documents.azure.com:443/",
    "DatabaseName": "your-database-name",
    "Containers": {
      "Examples": "examples"
    }
  }
}
```

2. **Authenticate with Azure** (for Managed Identity):

```bash
az login
az account set --subscription "your-subscription-id"
```

### Running the Application

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project DataLayer.API.Examples

# Access Swagger UI
# https://localhost:<port>/swagger
```

## API Endpoints

The API exposes the following endpoints for managing examples:

- `GET /api/examples` - Retrieve all examples
- `GET /api/examples/{id}?category={category}` - Get example by ID and partition key
- `GET /api/examples/category/{category}` - Get examples by category
- `GET /api/examples/in-stock` - Get all in-stock examples
- `POST /api/examples` - Create a new example
- `PUT /api/examples/{id}` - Update an existing example
- `DELETE /api/examples/{id}?category={category}` - Delete an example

## Architecture

### Repository Pattern

The application uses a generic repository pattern with a base class that provides common CRUD operations:

- **`ICosmosDbRepository<T>`** - Generic repository interface
- **`CosmosRepositoryBase<T>`** - Base implementation with standard operations
- **`ExampleRepository`** - Specific repository with custom queries

### Service Layer

Business logic is encapsulated in service classes:

- **`IExampleService`** - Service interface
- **`ExampleService`** - Service implementation with business logic

### Cosmos DB Configuration

- **Connection Mode**: Gateway (firewall-friendly)
- **Authentication**: Managed Identity via `DefaultAzureCredential`
- **Serialization**: Camel case property naming
- **Partition Key**: `category` field on the `Example` entity

## Testing

This project includes comprehensive unit and integration tests. For detailed testing instructions, see [TESTING.md](TESTING.md).

### Quick Test Commands

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test DataLayer.API.Examples.UnitTests

# Run integration tests (requires Azure authentication)
dotnet test DataLayer.API.Examples.IntegrationTests
```

For integration test setup and authentication details, see the [Integration Tests README](DataLayer.API.Examples.IntegrationTests/README.md).

## Key Dependencies

- **Microsoft.Azure.Cosmos** (3.56.0) - Cosmos DB SDK
- **Azure.Identity** (1.13.1) - Azure authentication
- **Swashbuckle.AspNetCore** (6.5.0) - API documentation
- **xUnit** - Testing framework
- **Moq** - Mocking library for unit tests
- **FluentAssertions** - Fluent test assertions

## Authentication

The application uses `DefaultAzureCredential` which supports multiple authentication methods in the following order:

1. **Environment Variables** - `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`
2. **Managed Identity** - For Azure-hosted applications (App Service, Container Apps, etc.)
3. **Azure CLI** - For local development (`az login`)
4. **Visual Studio** - Authenticated user in Visual Studio
5. **VS Code** - Azure Account extension

## Extending the Application

### Adding a New Entity

1. Create the entity class in `Entities/`
2. Create repository interface and implementation in `Repositories/`
3. Create service interface and implementation in `Services/`
4. Create controller in `Controllers/`
5. Register repository and service in `ServiceCollectionExtensions.cs`
6. Add container name to `CosmosDbConfig.ContainersConfig`

### Example:

```csharp
// In CosmosDbExtensions.cs
services.AddScoped<IOrderRepository, OrderRepository>();

// In ServiceCollectionExtensions.cs
services.AddScoped<IOrderService, OrderService>();
```

## Best Practices Implemented

- ✅ Async/await throughout the stack
- ✅ Dependency injection for all dependencies
- ✅ Structured logging with `ILogger<T>`
- ✅ Configuration validation at startup
- ✅ XML documentation comments
- ✅ RESTful API design
- ✅ Proper HTTP status codes
- ✅ Partition key usage for efficient queries
- ✅ Generic repository pattern for code reuse
- ✅ Service layer for business logic isolation

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
