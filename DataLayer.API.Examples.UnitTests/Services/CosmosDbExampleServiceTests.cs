using DataLayer.API.Examples.Entities;
using DataLayer.API.Examples.Repositories;
using DataLayer.API.Examples.Services;
using Microsoft.Extensions.Logging;

namespace DataLayer.API.Examples.UnitTests.Services;

/// <summary>
/// Unit tests for the CosmosDbExampleService class.
/// </summary>
public class CosmosDbExampleServiceTests
{
    private readonly Mock<ICosmosDbExampleRepository> _mockRepository;
    private readonly Mock<ILogger<CosmosDbExampleService>> _mockLogger;
    private readonly CosmosDbExampleService _service;

    public CosmosDbExampleServiceTests()
    {
        _mockRepository = new Mock<ICosmosDbExampleRepository>();
        _mockLogger = new Mock<ILogger<CosmosDbExampleService>>();
        _service = new CosmosDbExampleService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllExamplesAsync_ShouldReturnAllExamples()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "Example 1", Category = "Electronics", Price = 99.99m, InStock = true },
            new() { Id = "2", Name = "Example 2", Category = "Books", Price = 29.99m, InStock = true }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(expectedExamples);

        // Act
        var result = await _service.GetAllExamplesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedExamples);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetExampleByIdAsync_WhenExampleExists_ShouldReturnExample()
    {
        // Arrange
        var expectedExample = new CosmosDbExample 
        { 
            Id = "1", 
            Name = "Test Example", 
            Category = "Electronics", 
            Price = 99.99m 
        };
        _mockRepository
            .Setup(r => r.GetByIdAsync("1", "Electronics"))
            .ReturnsAsync(expectedExample);

        // Act
        var result = await _service.GetExampleByIdAsync("1", "Electronics");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedExample);
        _mockRepository.Verify(r => r.GetByIdAsync("1", "Electronics"), Times.Once);
    }

    [Fact]
    public async Task GetExampleByIdAsync_WhenExampleDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync("999", "NonExistent"))
            .ReturnsAsync((CosmosDbExample?)null);

        // Act
        var result = await _service.GetExampleByIdAsync("999", "NonExistent");

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync("999", "NonExistent"), Times.Once);
    }

    [Fact]
    public async Task GetExamplesByCategoryAsync_ShouldReturnExamplesInCategory()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "Example 1", Category = "Electronics", Price = 99.99m },
            new() { Id = "2", Name = "Example 2", Category = "Electronics", Price = 149.99m }
        };
        _mockRepository
            .Setup(r => r.GetExamplesByCategoryAsync("Electronics"))
            .ReturnsAsync(expectedExamples);

        // Act
        var result = await _service.GetExamplesByCategoryAsync("Electronics");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Category.Should().Be("Electronics"));
        _mockRepository.Verify(r => r.GetExamplesByCategoryAsync("Electronics"), Times.Once);
    }

    [Fact]
    public async Task GetInStockExamplesAsync_ShouldReturnOnlyInStockExamples()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "In Stock 1", Category = "Electronics", InStock = true },
            new() { Id = "2", Name = "In Stock 2", Category = "Books", InStock = true }
        };
        _mockRepository
            .Setup(r => r.GetInStockExamplesAsync())
            .ReturnsAsync(expectedExamples);

        // Act
        var result = await _service.GetInStockExamplesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.InStock.Should().BeTrue());
        _mockRepository.Verify(r => r.GetInStockExamplesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateExampleAsync_ShouldSetTimestampsAndCallRepository()
    {
        // Arrange
        var newExample = new CosmosDbExample
        {
            Id = "1",
            Name = "New Example",
            Category = "Electronics",
            Price = 99.99m
        };
        var beforeCreation = DateTime.UtcNow;
        
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<CosmosDbExample>(), It.IsAny<string>()))
            .ReturnsAsync((CosmosDbExample e, string pk) => e);

        // Act
        var result = await _service.CreateExampleAsync(newExample);

        // Assert
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        result.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        _mockRepository.Verify(
            r => r.CreateAsync(It.Is<CosmosDbExample>(e => e.Id == "1" && e.Name == "New Example"), "Electronics"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateExampleAsync_ShouldSetIdAndUpdateTimestamp()
    {
        // Arrange
        var updatedExample = new CosmosDbExample
        {
            Name = "Updated Example",
            Category = "Electronics",
            Price = 149.99m
        };
        var beforeUpdate = DateTime.UtcNow;
        
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<CosmosDbExample>(), It.IsAny<string>()))
            .ReturnsAsync((string id, CosmosDbExample e, string pk) => e);

        // Act
        var result = await _service.UpdateExampleAsync("123", updatedExample);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("123");
        result.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        _mockRepository.Verify(
            r => r.UpdateAsync("123", It.Is<CosmosDbExample>(e => e.Id == "123"), "Electronics"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteExampleAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.DeleteAsync("123", "Electronics"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteExampleAsync("123", "Electronics");

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync("123", "Electronics"), Times.Once);
    }
}
