# Integration Tests Configuration

This directory contains integration tests that test the API endpoints with a real Cosmos DB connection.

## Prerequisites

The integration tests require authentication to Azure Cosmos DB. Your application uses `DefaultAzureCredential` for authentication.

### Option 1: Authenticate with Azure CLI (Recommended for Development)

```bash
# Login to Azure
az login

# Set the subscription if you have multiple
az account set --subscription "your-subscription-id"

# Run the tests
dotnet test DataLayer.API.Examples.IntegrationTests
```

### Option 2: Use Environment Variables

Set the following environment variables:

```bash
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
```

### Option 3: Use Managed Identity

If running in Azure (App Service, Container Apps, etc.), the Managed Identity will automatically authenticate.

## Current Test Status

✅ **1 test passing** - Swagger endpoint (no DB required)  
❌ **7 tests failing** - Require Azure authentication

The tests are correctly configured but need Azure authentication to access Cosmos DB at:
- Account: `https://systm1-data-db-doc-server.documents.azure.com:443/`
- Database: `systm1-data-db-doc`
- Container: `examples`

## Running the Tests

Once authenticated with Azure CLI:

```bash
# Run all integration tests
dotnet test DataLayer.API.Examples.IntegrationTests

# Run with detailed output
dotnet test DataLayer.API.Examples.IntegrationTests --verbosity normal

# Run only specific test
dotnet test DataLayer.API.Examples.IntegrationTests --filter "FullyQualifiedName~GetAll"
```

## Test Coverage

The integration tests cover:
- ✅ GET `/api/examples` - Retrieve all examples
- ✅ GET `/api/examples/{id}` - Retrieve example by ID
- ✅ GET `/api/examples/category/{category}` - Retrieve by category
- ✅ GET `/api/examples/in-stock` - Retrieve in-stock examples
- ✅ POST `/api/examples` - Create new example
- ✅ PUT `/api/examples/{id}` - Update existing example
- ✅ DELETE `/api/examples/{id}` - Delete example

## Troubleshooting

### Authentication Errors

If you see errors like "DefaultAzureCredentialFailed to retrieve a token", you need to authenticate:

```bash
az login
```

### Connection Errors

Ensure your IP address is allowed in the Cosmos DB firewall rules or that "Allow access from Azure Portal" is enabled.

## Test Data Cleanup

Integration tests now include **automatic cleanup** of all test data:

- **Per-Test Cleanup**: Each test that creates data automatically tracks and removes it after completion
- **Collection Cleanup**: Before and after all tests run, a batch cleanup removes any leftover "IntegrationTest" category items
- **Failure-Safe**: Even if a test fails, cleanup runs to prevent data accumulation

All test data uses the category `"IntegrationTest"` for easy identification and cleanup. No manual cleanup is needed.

