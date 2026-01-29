# Test Data Cleanup Implementation

This document describes the automatic cleanup implementation for integration tests.

## Overview

The integration tests have been updated to automatically clean up all test data from Cosmos DB, preventing data accumulation and ensuring a clean test environment.

## Architecture

### 1. **IntegrationTestBase** (`TestBase/IntegrationTestBase.cs`)
- Base class for all integration tests
- Implements `IAsyncLifetime` for automatic setup/teardown
- Tracks items created during each test
- Automatically deletes tracked items after each test completes
- Provides `TrackCreatedItem()` methods for manual tracking

### 2. **TestCollectionFixture** (`TestBase/TestCollectionFixture.cs`)
- Shared fixture across all tests in a collection
- Performs batch cleanup before and after all tests run
- Removes all items with category "IntegrationTest"
- Handles leftover data from previous failed test runs

### 3. **TestWebApplicationFactory** (`TestBase/TestWebApplicationFactory.cs`)
- Custom WebApplicationFactory configured for testing environment
- Loads `appsettings.Testing.json` configuration
- Provides properly configured application instance for tests

## How It Works

### Per-Test Cleanup
1. Test creates data via API
2. Test calls `TrackCreatedItem()` to register the item
3. After test completes (success or failure), `DisposeAsync()` runs
4. All tracked items are deleted from Cosmos DB

### Collection-Level Cleanup
1. Before any test runs, `TestCollectionFixture.InitializeAsync()` executes
2. Queries Cosmos DB for all items with category "IntegrationTest"
3. Deletes any leftover items from previous runs
4. After all tests complete, the same cleanup runs again

## Usage Example

```csharp
[Collection("Integration Tests")]
public class MyApiTests : IntegrationTestBase
{
    public MyApiTests(TestCollectionFixture fixture) : base(fixture.Factory)
    {
    }

    [Fact]
    public async Task CreateItem_ShouldSucceed()
    {
        // Arrange
        var item = new Example { Category = "IntegrationTest", ... };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/examples", item);
        var created = await response.Content.ReadFromJsonAsync<Example>();
        
        // Track for cleanup
        TrackCreatedItem(created);
        
        // Assert
        created.Should().NotBeNull();
        
        // Cleanup happens automatically via DisposeAsync()
    }
}
```

## Benefits

1. **No Manual Cleanup Required**: All test data is automatically removed
2. **Failure-Safe**: Even if tests fail, cleanup still runs
3. **Clean State**: Each test starts with a predictable database state
4. **Batch Efficiency**: Collection-level cleanup handles leftover data in bulk
5. **Easy to Use**: Just inherit from `IntegrationTestBase` and call `TrackCreatedItem()`

## Implementation Checklist

- [x] Created `IntegrationTestBase` with automatic cleanup
- [x] Created `TestCollectionFixture` for shared setup/teardown
- [x] Created `TestWebApplicationFactory` for test configuration
- [x] Updated `CosmosDbExamplesApiTests` to use new base class
- [x] Added cleanup tracking to all tests that create data
- [x] Added Microsoft.Azure.Cosmos package reference
- [x] Updated documentation

## Testing the Cleanup

To verify cleanup is working:

1. Run tests: `dotnet test DataLayer.API.Examples.IntegrationTests`
2. Check console output for cleanup messages
3. Query Cosmos DB for test data: `SELECT * FROM c WHERE c.category = "IntegrationTest"`
4. Should find no items after tests complete

## Troubleshooting

### Cleanup Not Working?
- Ensure tests inherit from `IntegrationTestBase`
- Verify `TrackCreatedItem()` is called after creating data
- Check that test class has `[Collection("Integration Tests")]` attribute
- Confirm Azure authentication is working

### Cleanup Errors in Console?
- Non-critical warnings are logged but don't fail tests
- "Item not found" errors during cleanup are expected (item already deleted)
- Other errors are logged but isolated to prevent test failures

## Future Enhancements

Potential improvements:
- Add cleanup for other container types if needed
- Implement transaction-based cleanup for better atomicity
- Add cleanup metrics/reporting
- Support for cleanup of related entities (if relationships exist)
