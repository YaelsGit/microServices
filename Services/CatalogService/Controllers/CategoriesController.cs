using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using CatalogService.Interfaces;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoriesService _categoriesService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoriesService categoriesService, ILogger<CategoriesController> logger)
    {
        _categoriesService = categoriesService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DtoCategories>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories()
    {
        _logger.LogInformation("Fetching all categories");
        var categoriesEnumerable = await _categoriesService.GetAllCategoriesAsync();
        var categories = (categoriesEnumerable ?? Enumerable.Empty<DtoCategories>()).ToList();

        return Ok(new ApiResponse<List<DtoCategories>>
        {
            Success = true,
            Message = "Categories retrieved successfully",
            Data = categories,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DtoCategories>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        _logger.LogInformation("Fetching category with ID: {CategoryId}", id);
        var category = await _categoriesService.GetCategoryByIdAsync(id);

        if (category == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Category with ID {id} not found.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<DtoCategories>
        {
            Success = true,
            Message = "Category retrieved successfully",
            Data = category,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] DtoCreateCategoryRequest request)
    {
        _logger.LogInformation("Creating new category: {CategoryName}", request.Name);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Category name is required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var categoryId = await _categoriesService.CreateCategoryAsync(request);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = "Category created successfully",
            Data = new { CategoryId = categoryId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] DtoUpdateCategoryRequest request)
    {
        _logger.LogInformation("Updating category with ID: {CategoryId}", id);

        var success = await _categoriesService.UpdateCategoryAsync(id, request);

        if (!success)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Category not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Category updated successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        _logger.LogInformation("Deleting category with ID: {CategoryId}", id);

        var result = await _categoriesService.DeleteCategoryAsync(id);

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Category not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Category deleted successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }
}
