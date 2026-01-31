using Microsoft.AspNetCore.Mvc;
using DataLayer.API.Examples.Entities;
using DataLayer.API.Examples.Services;

namespace DataLayer.API.Examples.Controllers;

/// <summary>
/// API controller for managing examples.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamplesController : ControllerBase
{
    private readonly IExampleService _exampleService;
    private readonly ILogger<ExamplesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExamplesController"/> class.
    /// </summary>
    /// <param name="exampleService">The example service.</param>
    /// <param name="logger">The logger instance.</param>
    public ExamplesController(
        IExampleService exampleService,
        ILogger<ExamplesController> logger)
    {
        _exampleService = exampleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all examples.
    /// </summary>
    /// <returns>A collection of all examples.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Example>>> GetAll()
    {
        var examples = await _exampleService.GetAllExamplesAsync();
        return Ok(examples);
    }

    /// <summary>
    /// Gets an example by its ID and category.
    /// </summary>
    /// <param name="id">The unique identifier of the example.</param>
    /// <param name="category">The category (partition key) of the example.</param>
    /// <returns>The example if found; otherwise, NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Example>> GetById(string id, [FromQuery] string category)
    {
        var example = await _exampleService.GetExampleByIdAsync(id, category);
        
        if (example == null)
            return NotFound();

        return Ok(example);
    }

    /// <summary>
    /// Gets all examples in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A collection of examples in the specified category.</returns>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<Example>>> GetByCategory(string category)
    {
        var examples = await _exampleService.GetExamplesByCategoryAsync(category);
        return Ok(examples);
    }

    /// <summary>
    /// Gets all examples that are currently in stock.
    /// </summary>
    /// <returns>A collection of in-stock examples.</returns>
    [HttpGet("in-stock")]
    public async Task<ActionResult<IEnumerable<Example>>> GetInStock()
    {
        var examples = await _exampleService.GetInStockExamplesAsync();
        return Ok(examples);
    }

    /// <summary>
    /// Creates a new example.
    /// </summary>
    /// <param name="example">The example to create.</param>
    /// <returns>The created example.</returns>
    [HttpPost]
    public async Task<ActionResult<Example>> Create([FromBody] Example example)
    {
        var created = await _exampleService.CreateExampleAsync(example);
        return CreatedAtAction(nameof(GetById), new { id = created.Id, category = created.Category }, created);
    }

    /// <summary>
    /// Updates an existing example.
    /// </summary>
    /// <param name="id">The unique identifier of the example to update.</param>
    /// <param name="example">The updated example data.</param>
    /// <returns>The updated example.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<Example>> Update(string id, [FromBody] Example example)
    {
        var updated = await _exampleService.UpdateExampleAsync(id, example);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes an example.
    /// </summary>
    /// <param name="id">The unique identifier of the example to delete.</param>
    /// <param name="category">The category (partition key) of the example.</param>
    /// <returns>NoContent on successful deletion.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string category)
    {
        await _exampleService.DeleteExampleAsync(id, category);
        return NoContent();
    }
}
