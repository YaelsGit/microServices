using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using CatalogService.Interfaces;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GiftsController : ControllerBase
{
    private readonly IGiftsService _giftsService;
    private readonly ILogger<GiftsController> _logger;

    public GiftsController(IGiftsService giftsService, ILogger<GiftsController> logger)
    {
        _giftsService = giftsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all gifts
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DtoGifts>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGifts()
    {
        _logger.LogInformation("Fetching all gifts");
        var gifts = await _giftsService.GetAllGiftsAsync();
        var giftList = gifts?.ToList() ?? new List<DtoGifts>();

        return Ok(new ApiResponse<List<DtoGifts>>
        {
            Success = true,
            Message = "Gifts retrieved successfully",
            Data = giftList,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get gift by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DtoGifts>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGiftById(int id)
    {
        _logger.LogInformation("Fetching gift with ID: {GiftId}", id);
        var gift = await _giftsService.GetGiftByIdAsync(id);

        if (gift == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Gift with ID {id} not found.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<DtoGifts>
        {
            Success = true,
            Message = "Gift retrieved successfully",
            Data = gift,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a new gift
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGift([FromBody] DtoCreateGiftRequest request)
    {
        _logger.LogInformation("Creating new gift: {GiftName}", request.Name);

        if (string.IsNullOrWhiteSpace(request.Name) || request.Quantity < 0 || request.Price <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Gift name is required, quantity must be >= 0, and price must be > 0.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var giftId = await _giftsService.CreateGiftAsync(request);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = "Gift created successfully",
            Data = new { GiftId = giftId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update an existing gift
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGift(int id, [FromBody] DtoUpdateGiftRequest request)
    {
        _logger.LogInformation("Updating gift with ID: {GiftId}", id);

        var result = await _giftsService.UpdateGiftAsync(id, request);

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Gift not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Gift updated successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete a gift
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGift(int id)
    {
        _logger.LogInformation("Deleting gift with ID: {GiftId}", id);

        var result = await _giftsService.DeleteGiftAsync(id);

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Gift not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Gift deleted successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get gifts by price range
    /// </summary>
    [HttpGet("price-range")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoGifts>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGiftsByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
    {
        _logger.LogInformation("Fetching gifts in price range: {MinPrice} - {MaxPrice}", minPrice, maxPrice);
        var gifts = await _giftsService.GetGiftsByPriceRangeAsync(minPrice, maxPrice);
        var giftList = gifts?.ToList() ?? new List<DtoGifts>();

        return Ok(new ApiResponse<List<DtoGifts>>
        {
            Success = true,
            Message = "Gifts retrieved successfully",
            Data = giftList,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get filtered gifts
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoGifts>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilteredGifts([FromBody] DtoGiftFilter filter)
    {
        _logger.LogInformation("Fetching filtered gifts");
        var gifts = await _giftsService.GetFilteredGiftsAsync(filter);
        var giftList = gifts?.ToList() ?? new List<DtoGifts>();

        return Ok(new ApiResponse<List<DtoGifts>>
        {
            Success = true,
            Message = "Gifts retrieved successfully",
            Data = giftList,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update gift quantity
    /// </summary>
    [HttpPatch("{id}/quantity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGiftQuantity(int id, [FromBody] DtoUpdateGiftQuantityRequest request)
    {
        _logger.LogInformation("Updating gift {GiftId} quantity by: {QuantityChange}", id, request.QuantityChange);

        var result = await _giftsService.UpdateGiftQuantityAsync(id, request.QuantityChange);

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Gift not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Gift quantity updated successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }
}
