// Import configuration models for Cosmos DB settings
using DataLayer.API.Examples.Configuration;
// Import extension methods for dependency injection setup
using DataLayer.API.Examples.Extensions;

// ==================================================================================
// WebApplication Builder Initialization
// ==================================================================================
//
// This initializes the host with default configuration sources loaded in the following order (later sources override earlier ones):
// 1. appsettings.json (base configuration)
// 2. appsettings.{Environment}.json (environment-specific overrides, e.g., appsettings.Development.json, appsettings.Production.json)
//    - Environment is determined by ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable
// 3. User Secrets (in Development environment only, for storing sensitive data locally)
// 4. Environment variables (can override any appsettings values)
// 5. Command-line arguments (highest priority, can override all other sources)
var builder = WebApplication.CreateBuilder(args);


// ==================================================================================
// Configuration Loading
// ==================================================================================

// Load Cosmos DB configuration from appsettings.json or appsettings.{Environment}.json when either of the ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variables is set
// The configuration section name is defined in CosmosDbConfig.SectionName
// This retrieves connection strings, database name, container name, and other Cosmos DB settings
// Throws an exception if the configuration section is missing to fail fast during startup
var cosmosDbConfig = builder.Configuration
    .GetSection(CosmosDbConfig.SectionName)
    .Get<CosmosDbConfig>() ?? throw new InvalidOperationException("CosmosDb configuration is missing");


// ==================================================================================
// Service Registration
// ==================================================================================

// Add MVC controllers to the dependency injection container
// This enables the use of controller-based endpoints with model binding and validation
builder.Services.AddControllers();

// Add API Explorer services required for OpenAPI/Swagger documentation generation
// This scans controllers and generates metadata about available endpoints
builder.Services.AddEndpointsApiExplorer();

// Add Swagger generator services to create OpenAPI specification documents
// This enables interactive API documentation and testing UI
builder.Services.AddSwaggerGen();


// ==================================================================================
// Routing Configuration
// ==================================================================================

// Configure routing options to enforce consistent URL formatting
// LowercaseUrls: Converts all generated URLs to lowercase (e.g., /api/examples instead of /api/Examples)
// LowercaseQueryStrings: Keeps query string parameters in their original case for compatibility
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});


// ==================================================================================
// Infrastructure Services Registration
// ==================================================================================

// Register application infrastructure services including:
// - Cosmos DB client with proper configuration
// - Repository pattern implementations (CosmosDbExampleRepository, etc.)
// - Service layer implementations (CosmosDbExampleService, etc.)
// - Any other cross-cutting concerns defined in the extension method
builder.Services.AddInfrastructureExtensions(builder.Configuration, cosmosDbConfig);


// ==================================================================================
// Application Pipeline Configuration
// ==================================================================================

// Build the application from the configured services
// After this point, no more services can be registered
var app = builder.Build();

// Configure middleware pipeline for Development environment only
// Swagger UI should not be exposed in production for security reasons
if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated OpenAPI specification as JSON endpoint
    app.UseSwagger();
    
    // Enable middleware to serve Swagger UI (HTML, JS, CSS, etc.)
    // Provides an interactive interface for exploring and testing the API
    app.UseSwaggerUI();
}

// Add HTTPS redirection middleware
// Automatically redirects HTTP requests to HTTPS for secure communication
app.UseHttpsRedirection();

// Add authorization middleware to the request pipeline
// Evaluates authorization policies and ensures authenticated/authorized access to protected endpoints
app.UseAuthorization();

// Map controller endpoints to the request pipeline
// This discovers all controllers and creates routes based on their attributes
app.MapControllers();

// Start the web application and begin listening for incoming requests
// This call blocks until the application is shut down
app.Run();


// ==================================================================================
// Test Support
// ==================================================================================

// Expose the implicit Program class as public partial
// This allows integration test projects to reference the Program class
// for WebApplicationFactory<Program> to create test servers
public partial class Program { }
