using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using LotteryService.Interfaces;

namespace LotteryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotteryController : ControllerBase
{
    private readonly ILotteryService _lotteryService;
    private readonly ILogger<LotteryController> _logger;

    public LotteryController(ILotteryService lotteryService, ILogger<LotteryController> logger)
    {
        _lotteryService = lotteryService;
        _logger = logger;
    }

    /// <summary>
    /// Create a lottery ticket
    /// </summary>
    [HttpPost("tickets")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLotteryTicket([FromBody] dynamic request)
    {
        var userId = int.Parse(request?.userId?.ToString() ?? "0");
        var giftId = int.Parse(request?.giftId?.ToString() ?? "0");

        _logger.LogInformation($"Creating lottery ticket for user: {userId}, gift: {giftId}");

        if (userId <= 0 || giftId <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "UserId and GiftId are required and must be greater than 0.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var result = await _lotteryService.CreateLotteryTicketAsync(userId, giftId);

        if (!result.Success)
        {
            _logger.LogWarning($"Lottery ticket creation failed: {result.Message}");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Message,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation($"Lottery ticket created successfully with ID: {result.TicketId}");
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = result.Message,
            Data = new { TicketId = result.TicketId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Run the lottery draw (select winners)
    /// </summary>
    [HttpPost("draw")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunLotteryDraw()
    {
        _logger.LogInformation("Running lottery draw");

        var (success, winnerCount, message) = await _lotteryService.RunLotteryAsync();

        return Ok(new ApiResponse<object>
        {
            Success = success,
            Message = message,
            Data = new { WinnerCount = winnerCount },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get all lottery winners
    /// </summary>
    [HttpGet("winners")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoLotteryWinner>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWinners()
    {
        _logger.LogInformation("Fetching lottery winners");
        var winners = await _lotteryService.GetWinnersAsync();

        return Ok(new ApiResponse<List<DtoLotteryWinner>>
        {
            Success = true,
            Message = "Winners retrieved successfully",
            Data = winners.ToList(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get lottery tickets for a user
    /// </summary>
    [HttpGet("user/{userId}/tickets")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoLottery>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTickets(int userId)
    {
        _logger.LogInformation("Fetching lottery tickets for user: {0}", userId);
        var tickets = await _lotteryService.GetUserLotteryTicketsAsync(userId);

        return Ok(new ApiResponse<List<DtoLottery>>
        {
            Success = true,
            Message = "User lottery tickets retrieved successfully",
            Data = tickets.ToList(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get lottery statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<DtoLotteryStatistics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        _logger.LogInformation("Fetching lottery statistics");
        var statistics = await _lotteryService.GetLotteryStatisticsAsync();

        return Ok(new ApiResponse<DtoLotteryStatistics>
        {
            Success = true,
            Message = "Lottery statistics retrieved successfully",
            Data = statistics,
            Timestamp = DateTime.UtcNow
        });
    }
}
