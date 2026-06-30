using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using CatalogService.Interfaces;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonorsController : ControllerBase
{
    private readonly IDonorsService _donorsService;
    private readonly ILogger<DonorsController> _logger;

    public DonorsController(IDonorsService donorsService, ILogger<DonorsController> logger)
    {
        _donorsService = donorsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all donors
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DtoDonors>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDonors()
    {
        _logger.LogInformation("Fetching all donors");
        var donors = await _donorsService.GetAllDonorsAsync();

        return Ok(new ApiResponse<List<DtoDonors>>
        {
            Success = true,
            Message = "Donors retrieved successfully",
            Data = donors.ToList(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get donor by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DtoDonors>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDonorById(int id)
    {
        _logger.LogInformation("Fetching donor with ID: {DonorId}", id);
        var donor = await _donorsService.GetDonorByIdAsync(id);

        if (donor == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Donor with ID {id} not found.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<DtoDonors>
        {
            Success = true,
            Message = "Donor retrieved successfully",
            Data = donor,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a new donor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDonor([FromBody] DtoCreateDonorRequest request)
    {
        _logger.LogInformation("Creating new donor: {DonorName}", request.Name);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Donor name is required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var donorId = await _donorsService.CreateDonorAsync(request);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = "Donor created successfully",
            Data = new { DonorId = donorId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update an existing donor
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDonor(int id, [FromBody] DtoUpdateDonorRequest request)
    {
        _logger.LogInformation("Updating donor with ID: {DonorId}", id);

        var success = await _donorsService.UpdateDonorAsync(id, request);

        if (!success)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Donor not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Donor updated successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete a donor
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDonor(int id)
    {
        _logger.LogInformation("Deleting donor with ID: {DonorId}", id);

        var success = await _donorsService.DeleteDonorAsync(id);

        if (!success)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Donor not found",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Donor deleted successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }
}
