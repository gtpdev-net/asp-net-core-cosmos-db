using DataLayer.API.Examples.Controllers;
using DataLayer.API.Examples.Entities;
using DataLayer.API.Examples.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataLayer.API.Examples.UnitTests.Controllers;

/// <summary>
/// Unit tests for the CosmosDbExamplesController class.
/// </summary>
public class CosmosDbExamplesControllerTests
{
    private readonly Mock<ICosmosDbExampleService> _mockService;
    private readonly Mock<ILogger<CosmosDbExamplesController>> _mockLogger;
    private readonly CosmosDbExamplesController _controller;

    public CosmosDbExamplesControllerTests()
    {
        _mockService = new Mock<ICosmosDbExampleService>();
        _mockLogger = new Mock<ILogger<CosmosDbExamplesController>>();
        _controller = new CosmosDbExamplesController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithAllExamples()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "Example 1", Category = "Electronics" },
            new() { Id = "2", Name = "Example 2", Category = "Books" }
        };
        _mockService.Setup(s => s.GetAllExamplesAsync()).ReturnsAsync(expectedExamples);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<CosmosDbExample>>().Subject;
        returnValue.Should().HaveCount(2);
        returnValue.Should().BeEquivalentTo(expectedExamples);
    }

    [Fact]
    public async Task GetById_WhenExampleExists_ShouldReturnOkWithExample()
    {
        // Arrange
        var expectedExample = new CosmosDbExample 
        { 
            Id = "1", 
            Name = "Test Example", 
            Category = "Electronics" 
        };
        _mockService
            .Setup(s => s.GetExampleByIdAsync("1", "Electronics"))
            .ReturnsAsync(expectedExample);

        // Act
        var result = await _controller.GetById("1", "Electronics");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeOfType<CosmosDbExample>().Subject;
        returnValue.Should().BeEquivalentTo(expectedExample);
    }

    [Fact]
    public async Task GetById_WhenExampleDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetExampleByIdAsync("999", "NonExistent"))
            .ReturnsAsync((CosmosDbExample?)null);

        // Act
        var result = await _controller.GetById("999", "NonExistent");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByCategory_ShouldReturnOkWithFilteredExamples()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "Example 1", Category = "Electronics" },
            new() { Id = "2", Name = "Example 2", Category = "Electronics" }
        };
        _mockService
            .Setup(s => s.GetExamplesByCategoryAsync("Electronics"))
            .ReturnsAsync(expectedExamples);

        // Act
        var result = await _controller.GetByCategory("Electronics");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<CosmosDbExample>>().Subject;
        returnValue.Should().HaveCount(2);
        returnValue.Should().AllSatisfy(e => e.Category.Should().Be("Electronics"));
    }

    [Fact]
    public async Task GetInStock_ShouldReturnOkWithInStockExamples()
    {
        // Arrange
        var expectedExamples = new List<CosmosDbExample>
        {
            new() { Id = "1", Name = "In Stock 1", InStock = true },
            new() { Id = "2", Name = "In Stock 2", InStock = true }
        };
        _mockService
            .Setup(s => s.GetInStockExamplesAsync())
            .ReturnsAsync(expectedExamples);

        // Act
        var result = await _controller.GetInStock();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<CosmosDbExample>>().Subject;
        returnValue.Should().HaveCount(2);
        returnValue.Should().AllSatisfy(e => e.InStock.Should().BeTrue());
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtActionWithNewExample()
    {
        // Arrange
        var newExample = new CosmosDbExample
        {
            Id = "1",
            Name = "New Example",
            Category = "Electronics",
            Price = 99.99m
        };
        _mockService
            .Setup(s => s.CreateExampleAsync(It.IsAny<CosmosDbExample>()))
            .ReturnsAsync(newExample);

        // Act
        var result = await _controller.Create(newExample);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be("1");
        createdResult.RouteValues.Should().ContainKey("category").WhoseValue.Should().Be("Electronics");
        var returnValue = createdResult.Value.Should().BeOfType<CosmosDbExample>().Subject;
        returnValue.Should().BeEquivalentTo(newExample);
    }

    [Fact]
    public async Task Update_ShouldReturnOkWithUpdatedExample()
    {
        // Arrange
        var updatedExample = new CosmosDbExample
        {
            Id = "1",
            Name = "Updated Example",
            Category = "Electronics",
            Price = 149.99m
        };
        _mockService
            .Setup(s => s.UpdateExampleAsync("1", It.IsAny<CosmosDbExample>()))
            .ReturnsAsync(updatedExample);

        // Act
        var result = await _controller.Update("1", updatedExample);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeOfType<CosmosDbExample>().Subject;
        returnValue.Should().BeEquivalentTo(updatedExample);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeleteExampleAsync("1", "Electronics"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete("1", "Electronics");

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteExampleAsync("1", "Electronics"), Times.Once);
    }
}
